using Checkers.Application.Caching;
using Checkers.Application.Ports;
using Checkers.Application.Strength;
using Checkers.Domain;
using Microsoft.Extensions.Options;

namespace Checkers.Application.Moves;

/// <summary>The suggestion flow: parse, tablebase or engine search, legality check, cache.</summary>
public sealed class SuggestMoveService
{
    // The spec's threshold for answering from the endgame databases.
    private const int TablebasePieceLimit = 8;
    private const string PositionKeyPrefix = "pdn:";

    // The probe fixes the value; this short search only orders the moves that keep it,
    // because a win/draw/loss database has no distance to the win.
    private static readonly TimeSpan TablebaseTieBreakTime = TimeSpan.FromMilliseconds(10);

    // A tie-break that did not happen: no move to prefer, and no line or search statistics to report.
    private static readonly SearchResult NoTieBreak = new(string.Empty, [], 0, 0, 0, false);

    private readonly IEngineWorkerPool _workers;
    private readonly StrengthPolicy _policy;
    private readonly LruCache<SuggestionKey, MoveSuggestion> _cache;

    public SuggestMoveService(
        IEngineWorkerPool workers,
        StrengthPolicy policy,
        IOptions<CacheOptions> cacheOptions,
        TimeProvider timeProvider)
    {
        _workers = workers;
        _policy = policy;
        var options = cacheOptions.Value;
        _cache = new LruCache<SuggestionKey, MoveSuggestion>(
            options.Capacity,
            TimeSpan.FromMinutes(options.TtlMinutes),
            timeProvider);
    }

    /// <exception cref="InvalidPdnException">The position is not valid PDN.</exception>
    /// <exception cref="NoLegalMovesException">The side to move has no legal move.</exception>
    /// <exception cref="EngineMoveRejectedException">No engine move is legal in the position.</exception>
    public async Task<MoveSuggestion> SuggestAsync(
        string pdn,
        StrengthLevel? level,
        RequestedLimits? limits,
        CancellationToken cancellationToken)
    {
        var position = Position.Parse(pdn);
        var canonical = position.ToString();
        if (position.LegalMoves.Count == 0)
        {
            throw new NoLegalMovesException(canonical);
        }

        var key = new SuggestionKey(canonical, _policy.ResolveSearchLimits(level, limits));
        if (_cache.TryGet(key, out var cached))
        {
            return cached;
        }

        var suggestion = await ComputeAsync(position, key.Limits, cancellationToken);
        _cache.Set(key, suggestion);
        return suggestion;
    }

    private async Task<MoveSuggestion> ComputeAsync(
        Position position,
        SearchLimits limits,
        CancellationToken cancellationToken)
    {
        await using var session = await _workers.AcquireAsync(cancellationToken);
        if (position.PieceCount <= TablebasePieceLimit
            && await TrySolveFromTablebaseAsync(session, position, limits, cancellationToken) is { } solved)
        {
            return solved;
        }

        return await SearchWithEngineAsync(session, position, limits, cancellationToken);
    }

    private async Task<MoveSuggestion?> TrySolveFromTablebaseAsync(
        IEngineSession session,
        Position position,
        SearchLimits limits,
        CancellationToken cancellationToken)
    {
        if (await TablebaseSolver.SolveAsync(position, session, cancellationToken) is not { } verdict)
        {
            return null;
        }

        var tieBreak = await TieBreakAsync(session, position, limits, cancellationToken);
        var engineMove = Resolve(position, tieBreak.BestMove);
        var best = engineMove is not null && verdict.BestMoves.Contains(engineMove) ? engineMove : verdict.BestMoves[0];
        return CreateSuggestion(position, best, tieBreak, verdict.Score, tablebaseHit: true);
    }

    /// <summary>Searches briefly to pick among the proven moves; <see cref="NoTieBreak"/> when the engine fails.</summary>
    private static async Task<SearchResult> TieBreakAsync(
        IEngineAdapter engine,
        Position position,
        SearchLimits limits,
        CancellationToken cancellationToken)
    {
        var tieBreakLimits = limits with
        {
            MoveTime = limits.MoveTime < TablebaseTieBreakTime ? limits.MoveTime : TablebaseTieBreakTime,
        };
        try
        {
            return await SearchAsync(engine, position, tieBreakLimits, cancellationToken);
        }
        catch (EngineFailureException)
        {
            // The probe has already answered the request; without the tie-break any proven move will do.
            return NoTieBreak;
        }
    }

    private async Task<MoveSuggestion> SearchWithEngineAsync(
        IEngineAdapter engine,
        Position position,
        SearchLimits limits,
        CancellationToken cancellationToken)
    {
        var result = await SearchAsync(engine, position, limits, cancellationToken);
        string[] candidates = [result.BestMove, .. result.Pv];
        var best = candidates.Select(candidate => Resolve(position, candidate)).FirstOrDefault(move => move is not null)
            ?? throw new EngineMoveRejectedException(position.ToString(), candidates);
        return CreateSuggestion(position, best, result, result.ScoreOrWdl, result.TablebaseHit);
    }

    private static async Task<SearchResult> SearchAsync(
        IEngineAdapter engine,
        Position position,
        SearchLimits limits,
        CancellationToken cancellationToken)
    {
        await engine.SetPositionAsync(position, cancellationToken);
        return await engine.SearchAsync(limits, cancellationToken);
    }

    private MoveSuggestion CreateSuggestion(
        Position position,
        Move best,
        SearchResult search,
        int scoreOrWdl,
        bool tablebaseHit) =>
        new(
            _workers.EngineName,
            best,
            PrincipalVariation(position, best, search.Pv),
            scoreOrWdl,
            search.Depth,
            search.Nodes,
            PositionKeyPrefix + position,
            tablebaseHit);

    /// <summary>The legal prefix of the engine's line, or just <paramref name="best"/> when that line does not start with it.</summary>
    private static List<Move> PrincipalVariation(Position position, Move best, IReadOnlyList<string> pv)
    {
        var line = new List<Move>();
        var current = position;
        foreach (var notation in pv)
        {
            if (Resolve(current, notation) is not { } move)
            {
                break;
            }

            line.Add(move);
            current = current.Apply(move);
        }

        return line.Count > 0 && line[0].Equals(best) ? line : [best];
    }

    private static Move? Resolve(Position position, string notation) =>
        MoveNotation.TryParse(notation, out var parsed) ? position.FindLegalMove(parsed) : null;
}
