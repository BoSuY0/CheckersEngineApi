namespace Checkers.Domain;

public static class PieceColorExtensions
{
    public static PieceColor Opponent(this PieceColor color) =>
        color == PieceColor.Black ? PieceColor.White : PieceColor.Black;
}
