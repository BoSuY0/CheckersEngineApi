namespace Checkers.Domain.Tests;

public sealed class MoveGenerationTests
{
    private static string[] LegalMoves(string pdn) =>
        [.. Position.Parse(pdn).LegalMoves.Select(move => move.ToString())];

    [Fact]
    public void LegalMoves_InitialPosition_AreTheSevenBlackOpenings() =>
        Assert.Equal(
            ["9-13", "9-14", "10-14", "10-15", "11-15", "11-16", "12-16"],
            Position.Initial.LegalMoves.Select(move => move.ToString()));

    [Fact]
    public void LegalMoves_SpecSample_AreTheTwoBlackCaptures() =>
        Assert.Equal(["14x23", "16x23"], LegalMoves("B:W18,19,22,25,27,28,30,32:B1,5,6,7,10,12,14,16"));

    [Fact]
    public void LegalMoves_WhiteMen_MoveTowardLowerSquares() =>
        Assert.Equal(["18-14", "18-15"], LegalMoves("W:W18:B1"));

    [Fact]
    public void LegalMoves_Man_DoesNotMoveOrCaptureBackwards()
    {
        var position = Position.Parse("B:WK10:B14");

        Assert.Equal(["14-17", "14-18"], position.LegalMoves.Select(move => move.ToString()));
        Assert.False(position.HasCapture(PieceColor.Black));
        Assert.True(position.HasCapture(PieceColor.White));
    }

    [Fact]
    public void LegalMoves_King_MovesInEveryDirection() =>
        Assert.Equal(["14-17", "14-18", "14-9", "14-10"], LegalMoves("B:W32:BK14"));

    [Fact]
    public void LegalMoves_King_CapturesBackwards() =>
        Assert.Equal(["14x7"], LegalMoves("B:W10:BK14"));

    [Fact]
    public void LegalMoves_WhenACaptureExists_OnlyCapturesAreLegal()
    {
        var position = Position.Parse("B:W18:B1,14");

        Assert.Equal(["14x23"], position.LegalMoves.Select(move => move.ToString()));
        Assert.True(position.HasCapture(PieceColor.Black));
    }

    [Fact]
    public void LegalMoves_MultiJump_IsGeneratedOnlyToCompletion()
    {
        var move = Assert.Single(Position.Parse("B:W10,19:B6").LegalMoves);

        Assert.Equal("6x15x24", move.ToString());
        Assert.Equal([new Square(10), new Square(19)], move.Captured);
    }

    [Fact]
    public void LegalMoves_BranchingMultiJump_YieldsEveryCompleteSequence() =>
        Assert.Equal(["6x15x22", "6x15x24"], LegalMoves("B:W10,18,19:B6"));

    [Fact]
    public void LegalMoves_ShorterCaptureOfAnotherPiece_IsAlsoLegal() =>
        Assert.Equal(["6x15x24", "16x23"], LegalMoves("B:W10,19:B6,16"));

    [Fact]
    public void LegalMoves_ManCrownedDuringCapture_StopsThere() =>
        Assert.Equal(["22x31"], LegalMoves("B:W26,27:B22"));

    [Fact]
    public void LegalMoves_KingCircuit_MayEndOnItsStartSquare() =>
        Assert.Equal(["15x22x13x6x15", "15x6x13x22x15"], LegalMoves("B:W9,10,17,18:BK15"));

    [Theory]
    [InlineData("B:W5,6,10:B1")]
    [InlineData("B:W18:B")]
    public void LegalMoves_BlockedOrEmptySide_IsEmpty(string pdn) =>
        Assert.Empty(Position.Parse(pdn).LegalMoves);

    [Fact]
    public void Apply_Capture_RemovesCapturedPiecesAndPassesTheTurn()
    {
        var position = Position.Parse("B:WK10,19:B6");

        var next = position.Apply(position.LegalMoves[0]);

        Assert.Equal("W:W:B24", next.ToString());
    }

    [Fact]
    public void Apply_ManReachingCrowningRow_IsCrowned()
    {
        var position = Position.Parse("B:W26,27:B22");

        var next = position.Apply(position.LegalMoves[0]);

        Assert.Equal("W:W27:BK31", next.ToString());
    }

    [Fact]
    public void Apply_KingCircuit_KeepsTheKingOnItsStartSquare()
    {
        var position = Position.Parse("B:W9,10,17,18:BK15");

        var next = position.Apply(position.LegalMoves[0]);

        Assert.Equal("W:W:BK15", next.ToString());
    }

    [Fact]
    public void Apply_WhiteQuietMove_MovesThePiece()
    {
        var position = Position.Parse("W:WK18:B1");

        var next = position.Apply(position.LegalMoves[^1]);

        Assert.Equal("B:WK15:B1", next.ToString());
    }

    [Fact]
    public void Apply_MoveOfAnotherPosition_Throws()
    {
        var foreign = Position.Parse("B:W32:B5").LegalMoves[0];

        Assert.Throws<ArgumentException>(() => Position.Initial.Apply(foreign));
    }
}
