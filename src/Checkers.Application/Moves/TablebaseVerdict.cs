using Checkers.Application.Ports;
using Checkers.Domain;

namespace Checkers.Application.Moves;

/// <param name="Value">The value of the position for the side to move; never <see cref="Wdl.Unknown"/>.</param>
/// <param name="BestMoves">Every move that achieves <paramref name="Value"/>, in generation order.</param>
internal sealed record TablebaseVerdict(Wdl Value, IReadOnlyList<Move> BestMoves)
{
    /// <summary>The value as a score: 1 win, 0 draw, -1 loss.</summary>
    public int Score => Value switch
    {
        Wdl.Win => 1,
        Wdl.Loss => -1,
        _ => 0,
    };
}
