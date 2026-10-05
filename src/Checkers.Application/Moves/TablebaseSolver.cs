using Checkers.Application.Ports;
using Checkers.Domain;

namespace Checkers.Application.Moves;

/// <summary>
/// Solves a position from win/draw/loss database values. The database holds no valid value while either side
/// has a capture, so each move is first played out until the line is quiet, and the values are backed up by negamax.
/// </summary>
internal static class TablebaseSolver
{
    // Every capture removes a piece, so real capture sequences end long before this depth.
    private const int MaxResolutionPlies = 8;

    /// <summary>Probes every quiet leaf below the legal moves of <paramref name="position"/> in a single batch.</summary>
    /// <returns>The value and its best moves, or <see langword="null"/> when the database cannot decide.</returns>
    public static async Task<TablebaseVerdict?> SolveAsync(
        Position position,
        ITablebase tablebase,
        CancellationToken cancellationToken)
    {
        var leaves = new List<Position>();
        var moves = position.LegalMoves;
        var children = moves.Select(move => Expand(position.Apply(move), 1, leaves)).ToList();
        IReadOnlyList<Wdl> probed = leaves.Count == 0 ? [] : await tablebase.ProbeAsync(leaves, cancellationToken);

        var values = children.Select(child => Negate(child.Evaluate(probed))).ToList();
        var best = Best(values);
        return best == Wdl.Unknown
            ? null
            : new TablebaseVerdict(best, [.. moves.Where((_, index) => values[index] == best)]);
    }

    private static Node Expand(Position position, int ply, List<Position> leaves)
    {
        if (position.LegalMoves.Count == 0)
        {
            return new Settled(Wdl.Loss);
        }

        if (!position.HasCapture(PieceColor.Black) && !position.HasCapture(PieceColor.White))
        {
            leaves.Add(position);
            return new Leaf(leaves.Count - 1);
        }

        return ply == MaxResolutionPlies
            ? new Settled(Wdl.Unknown)
            : new Inner([.. position.LegalMoves.Select(move => Expand(position.Apply(move), ply + 1, leaves))]);
    }

    private static Wdl Negate(Wdl value) => value switch
    {
        Wdl.Win => Wdl.Loss,
        Wdl.Loss => Wdl.Win,
        _ => value,
    };

    // A win settles the node; otherwise an unknown option could hide a win, so the node is unknown.
    // Known values compare by their declaration order: Loss < Draw < Win.
    private static Wdl Best(IEnumerable<Wdl> options)
    {
        var best = Wdl.Loss;
        var anyUnknown = false;
        foreach (var option in options)
        {
            if (option == Wdl.Win)
            {
                return Wdl.Win;
            }

            if (option == Wdl.Unknown)
            {
                anyUnknown = true;
            }
            else if (option > best)
            {
                best = option;
            }
        }

        return anyUnknown ? Wdl.Unknown : best;
    }

    /// <summary>A position in the resolution tree, valued from its side to move's point of view.</summary>
    private abstract class Node
    {
        public abstract Wdl Evaluate(IReadOnlyList<Wdl> probed);
    }

    private sealed class Settled(Wdl value) : Node
    {
        public override Wdl Evaluate(IReadOnlyList<Wdl> probed) => value;
    }

    private sealed class Leaf(int probeIndex) : Node
    {
        public override Wdl Evaluate(IReadOnlyList<Wdl> probed) => probed[probeIndex];
    }

    private sealed class Inner(IReadOnlyList<Node> children) : Node
    {
        public override Wdl Evaluate(IReadOnlyList<Wdl> probed) =>
            Best(children.Select(child => Negate(child.Evaluate(probed))));
    }
}
