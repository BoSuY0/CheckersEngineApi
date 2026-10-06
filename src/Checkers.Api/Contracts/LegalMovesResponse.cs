using Checkers.Application.Moves;

namespace Checkers.Api.Contracts;

public sealed record LegalMovesResponse(string Position, IReadOnlyList<LegalMoveResponse> Moves)
{
    public static LegalMovesResponse From(PositionMoves moves) => new(
        moves.Position.ToString(),
        [.. moves.Moves.Select(legal => new LegalMoveResponse(legal.Move.ToString(), legal.Result.ToString()))]);
}
