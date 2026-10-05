using Checkers.Engine.KingsRowHost;
using Checkers.Engine.Protocol;

namespace Checkers.Engine.Tests.Host;

public sealed class KingsRowBoardTests
{
    private const int BlackMan = 6;
    private const int BlackKing = 10;
    private const int WhiteMan = 5;
    private const int WhiteKing = 9;

    // CheckerBoard's English numbering: square = 4 * (y + 1) - x / 2, with y = 0 on Black's back rank.
    [Theory]
    [InlineData(1, 6, 0)]
    [InlineData(4, 0, 0)]
    [InlineData(5, 7, 1)]
    [InlineData(29, 7, 7)]
    [InlineData(32, 1, 7)]
    public void ToCells_PutsTheSquareOnItsBoardCoordinates(int square, int x, int y)
    {
        var cells = KingsRowBoard.ToCells(new Board(Side.Black, [square], [], [], []));

        Assert.Equal(BlackMan, cells[(x * 8) + y]);
        Assert.Single(cells, cell => cell != 0);
    }

    [Fact]
    public void ToCells_EncodesColourAndKind()
    {
        var cells = KingsRowBoard.ToCells(new Board(Side.Black, [1], [2], [31], [32]));

        Assert.Equal(BlackMan, cells[(6 * 8) + 0]);
        Assert.Equal(BlackKing, cells[(4 * 8) + 0]);
        Assert.Equal(WhiteMan, cells[(3 * 8) + 7]);
        Assert.Equal(WhiteKing, cells[(1 * 8) + 7]);
    }

    [Theory]
    [MemberData(nameof(PlayedMoves))]
    public void FindMove_ReadsThePlayedMoveFromTheBoards(Board before, Board after, string expected) =>
        Assert.Equal(expected, KingsRowBoard.FindMove(KingsRowBoard.ToCells(before), KingsRowBoard.ToCells(after), before.ToMove));

    [Fact]
    public void FindMove_KingCircuitBackToItsStartSquare_IsNotVisible()
    {
        var before = new Board(Side.Black, [], [15], [9, 10, 17, 18], []);
        var after = new Board(Side.White, [], [15], [], []);

        Assert.Null(KingsRowBoard.FindMove(KingsRowBoard.ToCells(before), KingsRowBoard.ToCells(after), Side.Black));
    }

    public static TheoryData<Board, Board, string> PlayedMoves() => new()
    {
        { new Board(Side.Black, [11], [], [22], []), new Board(Side.White, [15], [], [22], []), "11-15" },
        { new Board(Side.Black, [14], [], [18, 30], []), new Board(Side.White, [23], [], [30], []), "14x23" },
        { new Board(Side.Black, [6], [], [10, 19], []), new Board(Side.White, [24], [], [], []), "6x24" },
        { new Board(Side.White, [12], [], [5], []), new Board(Side.Black, [12], [], [], [1]), "5-1" },
        { new Board(Side.Black, [22], [], [26, 27], []), new Board(Side.White, [], [31], [27], []), "22x31" },
    };
}
