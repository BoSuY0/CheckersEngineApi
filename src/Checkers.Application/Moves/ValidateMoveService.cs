using System.Diagnostics.CodeAnalysis;
using Checkers.Domain;

namespace Checkers.Application.Moves;

public sealed class ValidateMoveService
{
    /// <exception cref="InvalidPdnException">The position or the move is not valid PDN.</exception>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "A use case injected like the others.")]
    public bool IsLegal(string pdn, string move) =>
        Position.Parse(pdn).FindLegalMove(MoveNotation.Parse(move)) is not null;
}
