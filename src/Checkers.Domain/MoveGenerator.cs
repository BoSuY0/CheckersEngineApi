namespace Checkers.Domain;

/// <summary>
/// English/American checkers move generation. Moves come in a deterministic order: by start square ascending,
/// then by <see cref="BoardGeometry"/> direction order, depth first through multi-jumps.
/// </summary>
internal static class MoveGenerator
{
    /// <summary>The legal moves of the side to move. Captures are mandatory, so they exclude quiet moves.</summary>
    public static IReadOnlyList<Move> Generate(Position position)
    {
        var pieces = PiecesOf(position, position.SideToMove).ToList();
        var captures = pieces.SelectMany(own => new CaptureSearch(position, own.Square, own.Piece).Moves()).ToList();
        return captures.Count > 0 ? [.. captures] : [.. pieces.SelectMany(own => QuietMoves(position, own.Square, own.Piece))];
    }

    public static bool HasCapture(Position position, PieceColor color) =>
        PiecesOf(position, color).Any(own => new CaptureSearch(position, own.Square, own.Piece).CanStart());

    private static IEnumerable<Move> QuietMoves(Position position, Square from, Piece piece) =>
        BoardGeometry.Diagonals(from, piece)
            .Where(diagonal => position[diagonal.Adjacent] is null)
            .Select(diagonal => new Move([from, diagonal.Adjacent], []));

    private static IEnumerable<(Square Square, Piece Piece)> PiecesOf(Position position, PieceColor color)
    {
        foreach (var square in Square.All)
        {
            if (position[square] is { } piece && piece.Color == color)
            {
                yield return (square, piece);
            }
        }
    }

    /// <summary>Enumerates every complete jump sequence of one piece.</summary>
    private sealed class CaptureSearch(Position position, Square start, Piece piece)
    {
        public bool CanStart() => Jumps(start, []).Any();

        public IEnumerable<Move> Moves() => Continue([start], []);

        private IEnumerable<Move> Continue(IReadOnlyList<Square> path, IReadOnlyList<Square> captured)
        {
            foreach (var (jumped, landing) in Jumps(path[^1], captured))
            {
                IReadOnlyList<Square> nextPath = [.. path, landing];
                IReadOnlyList<Square> nextCaptured = [.. captured, jumped];
                if (IsCrowning(landing) || !Jumps(landing, nextCaptured).Any())
                {
                    yield return new Move(nextPath, nextCaptured);
                }
                else
                {
                    foreach (var move in Continue(nextPath, nextCaptured))
                    {
                        yield return move;
                    }
                }
            }
        }

        // Crowning ends the move, even in the middle of a multi-jump.
        private bool IsCrowning(Square landing) =>
            piece.Kind == PieceKind.Man && BoardGeometry.IsCrowningSquare(landing, piece.Color);

        // A piece cannot be jumped twice in one move.
        private IEnumerable<(Square Jumped, Square Landing)> Jumps(Square from, IReadOnlyList<Square> captured)
        {
            foreach (var diagonal in BoardGeometry.Diagonals(from, piece))
            {
                if (diagonal.Landing is { } landing
                    && position[diagonal.Adjacent]?.Color == piece.Color.Opponent()
                    && !captured.Contains(diagonal.Adjacent)
                    && IsVacant(landing))
                {
                    yield return (diagonal.Adjacent, landing);
                }
            }
        }

        // The moving piece has left its start square, so a king may land on it again.
        private bool IsVacant(Square square) => square == start || position[square] is null;
    }
}
