using Checkers.Engine.Protocol;

namespace Checkers.Engine.KingsRowHost;

/// <summary>
/// KingsRow's <c>int board[8][8]</c>, indexed <c>[x][y]</c> and flattened to <c>x * 8 + y</c>.
/// Row <c>y = 0</c> holds squares 1-4, Black's back rank.
/// </summary>
internal static class KingsRowBoard
{
    public const int CellCount = 64;

    private const int SquareCount = 32;
    private const int Empty = 0;
    private const int White = 1;
    private const int Black = 2;
    private const int Man = 4;
    private const int King = 8;

    /// <summary>The engine's colour code for <paramref name="side"/>.</summary>
    public static int Color(Side side) => side == Side.Black ? Black : White;

    public static int[] ToCells(Board board)
    {
        var cells = new int[CellCount];
        Place(cells, board.BlackMen, Black | Man);
        Place(cells, board.BlackKings, Black | King);
        Place(cells, board.WhiteMen, White | Man);
        Place(cells, board.WhiteKings, White | King);
        return cells;
    }

    /// <summary>Reads the move <paramref name="side"/> played from the boards before and after it, in short PDN notation.</summary>
    /// <returns>
    /// <c>from-to</c> or <c>fromxto</c>; <see langword="null"/> when the move is not visible in the difference,
    /// as for a king capture that ends on its start square, or when nothing moved.
    /// </returns>
    public static string? FindMove(ReadOnlySpan<int> before, ReadOnlySpan<int> after, Side side)
    {
        var own = Color(side);
        int? from = null, to = null;
        var capture = false;
        for (var square = 1; square <= SquareCount; square++)
        {
            var (previous, current) = (before[Index(square)], after[Index(square)]);
            if (previous == current)
            {
                continue;
            }

            if (current == Empty && (previous & own) != 0)
            {
                from = square;
            }
            else if (previous == Empty && (current & own) != 0)
            {
                to = square;
            }
            else if (current == Empty)
            {
                capture = true;
            }
        }

        return from is { } start && to is { } end ? $"{start}{(capture ? 'x' : '-')}{end}" : null;
    }

    private static void Place(int[] cells, IReadOnlyList<int> squares, int piece)
    {
        foreach (var square in squares)
        {
            cells[Index(square)] = piece;
        }
    }

    private static int Index(int square)
    {
        var offset = square - 1;
        var y = offset / 4;
        var x = (2 * (3 - (offset % 4))) + (y % 2);
        return (x * 8) + y;
    }
}
