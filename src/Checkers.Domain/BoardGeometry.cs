namespace Checkers.Domain;

/// <summary>
/// Board coordinates of the PDN squares. Row 0 holds squares 1-4 (Black's back row) and row 7 holds squares 29-32,
/// so Black men advance toward higher rows and White men toward lower ones.
/// </summary>
internal static class BoardGeometry
{
    private const int Size = 8;
    private const int SquaresPerRow = Size / 2;

    // This order fixes the order in which moves are generated.
    private static readonly (int Row, int Column)[] Directions = [(1, -1), (1, 1), (-1, -1), (-1, 1)];

    private static readonly IReadOnlyList<Diagonal>[] BlackManDiagonals = BuildTable(rowStep => rowStep > 0);
    private static readonly IReadOnlyList<Diagonal>[] WhiteManDiagonals = BuildTable(rowStep => rowStep < 0);
    private static readonly IReadOnlyList<Diagonal>[] KingDiagonals = BuildTable(_ => true);

    /// <summary>The diagonals along which <paramref name="piece"/> may move from <paramref name="square"/>, in generation order.</summary>
    public static IReadOnlyList<Diagonal> Diagonals(Square square, Piece piece)
    {
        var table = piece.Kind == PieceKind.King ? KingDiagonals
            : piece.Color == PieceColor.Black ? BlackManDiagonals
            : WhiteManDiagonals;
        return table[square.Number - 1];
    }

    /// <summary>True when a man of <paramref name="color"/> is crowned on <paramref name="square"/>.</summary>
    public static bool IsCrowningSquare(Square square, PieceColor color) =>
        Row(square) == (color == PieceColor.Black ? Size - 1 : 0);

    private static IReadOnlyList<Diagonal>[] BuildTable(Func<int, bool> allowsRowStep) =>
        [.. Square.All.Select(square => (IReadOnlyList<Diagonal>)
            [.. Directions.Where(direction => allowsRowStep(direction.Row)).SelectMany(direction => DiagonalsFrom(square, direction))])];

    private static IEnumerable<Diagonal> DiagonalsFrom(Square square, (int Row, int Column) direction)
    {
        var row = Row(square);
        var column = Column(square);
        if (At(row + direction.Row, column + direction.Column) is { } adjacent)
        {
            yield return new Diagonal(adjacent, At(row + (2 * direction.Row), column + (2 * direction.Column)));
        }
    }

    private static int Row(Square square) => (square.Number - 1) / SquaresPerRow;

    private static int Column(Square square)
    {
        var row = Row(square);
        return (2 * ((square.Number - 1) % SquaresPerRow)) + (row % 2 == 0 ? 1 : 0);
    }

    // Diagonal neighbours of a dark square are dark, so only the board edges need checking.
    private static Square? At(int row, int column) =>
        row is >= 0 and < Size && column is >= 0 and < Size
            ? new Square((row * SquaresPerRow) + (column / 2) + 1)
            : null;
}
