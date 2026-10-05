using Checkers.Application.Moves;
using Checkers.Application.Ports;
using Checkers.Application.Strength;
using Checkers.Application.Tests.Fakes;
using Checkers.Domain;
using Microsoft.Extensions.Time.Testing;

namespace Checkers.Application.Tests.Moves;

public sealed class SuggestMoveServiceTests
{
    private static readonly SearchResult MidgameResult = new(
        BestMove: "14x23",
        Pv: ["14x23", "27x18", "16x23", "9-14"],
        ScoreOrWdl: 35,
        Nodes: 153201,
        Depth: 14,
        TablebaseHit: false);

    private readonly FakeTimeProvider _time = new();

    [Fact]
    public async Task Suggest_Midgame_ReturnsVerifiedEngineAnswer()
    {
        var pool = new FakeEnginePool { Result = MidgameResult };

        var suggestion = await SuggestAsync(pool, "B:B1,5-7,10,12,14,16:W18,19,22,25,27,28,30,32", StrengthLevel.Strong);

        Assert.Equal(pool.EngineName, suggestion.Engine);
        Assert.Equal("14x23", suggestion.BestMove.ToString());
        Assert.Equal(["14x23", "27x18", "16x23"], suggestion.Pv.ToNotations());
        Assert.Equal(35, suggestion.ScoreOrWdl);
        Assert.Equal(14, suggestion.Depth);
        Assert.Equal(153201, suggestion.Nodes);
        Assert.Equal($"pdn:{TestPositions.Midgame}", suggestion.PositionKey);
        Assert.False(suggestion.TablebaseHit);
        var search = Assert.Single(pool.Searches);
        Assert.Equal(Position.Parse(TestPositions.Midgame), search.Position);
        Assert.Equal(new SearchLimits(TimeSpan.FromMilliseconds(500), 18), search.Limits);
        Assert.Equal(1, pool.Releases);
    }

    [Fact]
    public async Task Suggest_IllegalBestMove_FallsBackToNextPvCandidate()
    {
        var pool = new FakeEnginePool { Result = MidgameResult with { BestMove = "22-17", Pv = ["junk", "16x23"] } };

        var suggestion = await SuggestAsync(pool, TestPositions.Midgame);

        Assert.Equal("16x23", suggestion.BestMove.ToString());
        Assert.Equal(["16x23"], suggestion.Pv.ToNotations());
    }

    [Fact]
    public async Task Suggest_NoLegalCandidate_ThrowsEngineMoveRejectedAndReleasesWorker()
    {
        var pool = new FakeEnginePool { Result = MidgameResult with { BestMove = "10-15", Pv = ["10-15", "22-17"] } };

        await Assert.ThrowsAsync<EngineMoveRejectedException>(() => SuggestAsync(pool, TestPositions.Midgame));
        Assert.Equal(1, pool.Releases);
    }

    [Fact]
    public async Task Suggest_FailedSuggestion_IsNotCached()
    {
        var pool = new FakeEnginePool { Result = MidgameResult with { BestMove = "10-15", Pv = [] } };
        var service = CreateService(pool);

        await Assert.ThrowsAsync<EngineMoveRejectedException>(() => SuggestAsync(service, TestPositions.Midgame));
        await Assert.ThrowsAsync<EngineMoveRejectedException>(() => SuggestAsync(service, TestPositions.Midgame));

        Assert.Equal(2, pool.Acquisitions);
    }

    [Fact]
    public async Task Suggest_TablebasePosition_OverridesNonOptimalEngineMove()
    {
        var pool = new FakeEnginePool
        {
            Tablebase = new FakeTablebase(TestPositions.EndgameValues),
            Result = new SearchResult("1-5", ["1-5", "27-23"], ScoreOrWdl: 0, Nodes: 900, Depth: 6, TablebaseHit: true),
        };

        var suggestion = await SuggestAsync(pool, TestPositions.Endgame, StrengthLevel.Weak);

        Assert.Equal("1-6", suggestion.BestMove.ToString());
        Assert.Equal(["1-6"], suggestion.Pv.ToNotations());
        Assert.Equal(1, suggestion.ScoreOrWdl);
        Assert.True(suggestion.TablebaseHit);
        Assert.Equal(6, suggestion.Depth);
        Assert.Equal(900, suggestion.Nodes);
        Assert.Equal(new SearchLimits(TimeSpan.FromMilliseconds(10), 8), Assert.Single(pool.Searches).Limits);
    }

    [Fact]
    public async Task Suggest_TablebasePosition_KeepsOptimalEngineMoveAndItsLine()
    {
        var pool = new FakeEnginePool
        {
            Tablebase = new FakeTablebase(TestPositions.EndgameValues),
            Result = new SearchResult("19-23", ["19-23", "27x18", "1-5"], ScoreOrWdl: 2400, Nodes: 900, Depth: 6, TablebaseHit: true),
        };

        var suggestion = await SuggestAsync(pool, TestPositions.Endgame);

        Assert.Equal("19-23", suggestion.BestMove.ToString());
        Assert.Equal(["19-23", "27x18", "1-5"], suggestion.Pv.ToNotations());
        Assert.Equal(1, suggestion.ScoreOrWdl);
        Assert.True(suggestion.TablebaseHit);
    }

    [Fact]
    public async Task Suggest_TablebasePositionWhenTieBreakSearchFails_ReturnsAProvenMove()
    {
        var pool = new FakeEnginePool { Tablebase = new FakeTablebase(TestPositions.EndgameValues), FailsSearches = true };

        var suggestion = await SuggestAsync(pool, TestPositions.Endgame);

        Assert.Equal("1-6", suggestion.BestMove.ToString());
        Assert.Equal(["1-6"], suggestion.Pv.ToNotations());
        Assert.Equal(1, suggestion.ScoreOrWdl);
        Assert.True(suggestion.TablebaseHit);
        Assert.Equal(0, suggestion.Depth);
        Assert.Equal(0, suggestion.Nodes);
    }

    [Fact]
    public async Task Suggest_FailedEngineSearch_ThrowsAndReleasesWorker()
    {
        var pool = new FakeEnginePool { FailsSearches = true };

        await Assert.ThrowsAsync<EngineFailureException>(() => SuggestAsync(pool, TestPositions.Midgame));
        Assert.Equal(1, pool.Releases);
    }

    [Fact]
    public async Task Suggest_TablebaseCannotDecide_FallsBackToEngineSearch()
    {
        var pool = new FakeEnginePool
        {
            Result = new SearchResult("1-5", ["1-5"], ScoreOrWdl: 12, Nodes: 5000, Depth: 8, TablebaseHit: false),
        };

        var suggestion = await SuggestAsync(pool, TestPositions.Endgame, StrengthLevel.Weak);

        Assert.Equal("1-5", suggestion.BestMove.ToString());
        Assert.Equal(12, suggestion.ScoreOrWdl);
        Assert.False(suggestion.TablebaseHit);
        Assert.Equal(new SearchLimits(TimeSpan.FromMilliseconds(100), 8), Assert.Single(pool.Searches).Limits);
    }

    [Fact]
    public async Task Suggest_SamePositionInAnyNotation_IsServedFromCache()
    {
        var pool = new FakeEnginePool { Result = MidgameResult };
        var service = CreateService(pool);

        var first = await SuggestAsync(service, TestPositions.Midgame);
        var second = await SuggestAsync(service, " B : B16,14,12,10,5-7,1 : W32,30,27,28,25,22,19,18 ");

        Assert.Same(first, second);
        Assert.Equal(1, pool.Acquisitions);
    }

    [Fact]
    public async Task Suggest_DifferentSearchLimits_AreCachedSeparately()
    {
        var pool = new FakeEnginePool { Result = MidgameResult };
        var service = CreateService(pool);

        await SuggestAsync(service, TestPositions.Midgame, StrengthLevel.Weak);
        await SuggestAsync(service, TestPositions.Midgame, StrengthLevel.Strong);

        Assert.Equal(2, pool.Acquisitions);
    }

    [Fact]
    public async Task Suggest_DifferentHardTimeout_SharesCacheEntry()
    {
        var pool = new FakeEnginePool { Result = MidgameResult };
        var service = CreateService(pool);

        await SuggestAsync(service, TestPositions.Midgame, StrengthLevel.Weak);
        await SuggestAsync(service, TestPositions.Midgame, StrengthLevel.Weak, new RequestedLimits(null, null, HardTimeMs: 400));

        Assert.Equal(1, pool.Acquisitions);
    }

    [Fact]
    public async Task Suggest_AfterCacheTtl_SearchesAgain()
    {
        var pool = new FakeEnginePool { Result = MidgameResult };
        var service = CreateService(pool);

        await SuggestAsync(service, TestPositions.Midgame);
        _time.Advance(TimeSpan.FromMinutes(TestOptions.CacheTtlMinutes));
        await SuggestAsync(service, TestPositions.Midgame);

        Assert.Equal(2, pool.Acquisitions);
    }

    [Fact]
    public async Task Suggest_InvalidPdn_ThrowsWithoutAcquiringWorker()
    {
        var pool = new FakeEnginePool { Result = MidgameResult };

        await Assert.ThrowsAsync<InvalidPdnException>(() => SuggestAsync(pool, "B:W18,19:B18"));
        Assert.Equal(0, pool.Acquisitions);
    }

    [Fact]
    public async Task Suggest_NoLegalMove_ThrowsWithoutAcquiringWorker()
    {
        var pool = new FakeEnginePool { Result = MidgameResult };

        await Assert.ThrowsAsync<NoLegalMovesException>(() => SuggestAsync(pool, "B:WK10:B"));
        Assert.Equal(0, pool.Acquisitions);
    }

    private static Task<MoveSuggestion> SuggestAsync(
        SuggestMoveService service,
        string pdn,
        StrengthLevel? level = null,
        RequestedLimits? limits = null) =>
        service.SuggestAsync(pdn, level, limits, TestContext.Current.CancellationToken);

    private Task<MoveSuggestion> SuggestAsync(FakeEnginePool pool, string pdn, StrengthLevel? level = null) =>
        SuggestAsync(CreateService(pool), pdn, level);

    private SuggestMoveService CreateService(FakeEnginePool pool) =>
        new(pool, TestOptions.CreatePolicy(), TestOptions.Cache, _time);
}
