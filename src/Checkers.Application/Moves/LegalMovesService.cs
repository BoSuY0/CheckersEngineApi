using System.Diagnostics.CodeAnalysis;
using Checkers.Domain;

namespace Checkers.Application.Moves;

/// <summary>Lists the playable moves of a position, so that a client can play a game without rules of its own.</summary>
public sealed class LegalMovesService
{
    /// <exception cref="InvalidPdnException">The position is not valid PDN.</exception>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "A use case injected like the others.")]
    public PositionMoves List(string pdn)
    {
        var position = Position.Parse(pdn);
        return new PositionMoves(position, [.. position.LegalMoves.Select(move => new LegalMove(move, position.Apply(move)))]);
    }
}
