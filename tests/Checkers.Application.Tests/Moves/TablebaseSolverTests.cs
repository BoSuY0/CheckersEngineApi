using Checkers.Application.Moves;
using Checkers.Application.Ports;
using Checkers.Application.Tests.Fakes;
using Checkers.Domain;

namespace Checkers.Application.Tests.Moves;

public sealed class TablebaseSolverTests
{
    [Fact]
    public async Task Solve_ReturnsEveryMoveWithTheBestValue()
    {
        var verdict = await SolveAsync(TestPositions.Endgame, new FakeTablebase(TestPositions.EndgameValues));

        Assert.NotNull(verdict);
        Assert.Equal(Wdl.Win, verdict.Value);
        Assert.Equal(["1-6", "19-23"], verdict.BestMoves.ToNotations());
    }

    [Fact]
    public async Task Solve_ProbesExactlyTheQuietLeavesInOneBatch()
    {
        var tablebase = new FakeTablebase(TestPositions.EndgameValues);

        await SolveAsync(TestPositions.Endgame, tablebase);

        var batch = Assert.Single(tablebase.Batches);
        Assert.Equal(
            TestPositions.EndgameValues.Keys.Order(),
            batch.Select(position => position.ToString()).Order());
    }

    [Fact]
    public async Task Solve_PendingCapture_OpponentTakesItsBestCapture()
    {
        // 19-24 lets White choose between 27x20 (Black then wins) and 28x19 (a draw); every other move loses.
        var verdict = await SolveAsync(TestPositions.Endgame, new FakeTablebase(new Dictionary<string, Wdl>
        {
            ["W:W27,28:BK5,19"] = Wdl.Win,
            ["W:W27,28:BK6,19"] = Wdl.Win,
            ["B:W18,28:BK1"] = Wdl.Loss,
            ["B:W20,28:BK1"] = Wdl.Win,
            ["B:W19,27:BK1"] = Wdl.Draw,
        }));

        Assert.NotNull(verdict);
        Assert.Equal(Wdl.Draw, verdict.Value);
        Assert.Equal(["19-24"], verdict.BestMoves.ToNotations());
    }

    [Fact]
    public async Task Solve_UnknownMoveAndNoKnownWin_ReturnsNull()
    {
        var values = new Dictionary<string, Wdl>(TestPositions.EndgameValues)
        {
            ["W:W27,28:BK6,19"] = Wdl.Unknown,
            ["B:W18,28:BK1"] = Wdl.Draw,
        };

        Assert.Null(await SolveAsync(TestPositions.Endgame, new FakeTablebase(values)));
    }

    [Fact]
    public async Task Solve_KnownWin_IgnoresUnknownMoves()
    {
        var values = new Dictionary<string, Wdl>(TestPositions.EndgameValues)
        {
            ["W:W27,28:BK5,19"] = Wdl.Unknown,
            ["B:W18,28:BK1"] = Wdl.Unknown,
        };

        var verdict = await SolveAsync(TestPositions.Endgame, new FakeTablebase(values));

        Assert.NotNull(verdict);
        Assert.Equal(Wdl.Win, verdict.Value);
        Assert.Equal(["1-6"], verdict.BestMoves.ToNotations());
    }

    [Fact]
    public async Task Solve_LinesEndingWithoutMoves_AreDecidedWithoutProbing()
    {
        // Either move of the last black man is captured, leaving Black with nothing to move.
        var tablebase = new FakeTablebase();

        var verdict = await SolveAsync("B:W22:B14", tablebase);

        Assert.NotNull(verdict);
        Assert.Equal(Wdl.Loss, verdict.Value);
        Assert.Equal(["14-17", "14-18"], verdict.BestMoves.ToNotations());
        Assert.Empty(tablebase.Batches);
    }

    [Theory]
    [InlineData(Wdl.Win, 1)]
    [InlineData(Wdl.Draw, 0)]
    [InlineData(Wdl.Loss, -1)]
    public void VerdictScore_IsOneZeroMinusOne(Wdl value, int score) =>
        Assert.Equal(score, new TablebaseVerdict(value, []).Score);

    private static Task<TablebaseVerdict?> SolveAsync(string pdn, ITablebase tablebase) =>
        TablebaseSolver.SolveAsync(Position.Parse(pdn), tablebase, TestContext.Current.CancellationToken);
}
