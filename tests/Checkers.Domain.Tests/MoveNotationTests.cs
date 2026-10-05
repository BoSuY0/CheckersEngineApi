namespace Checkers.Domain.Tests;

public sealed class MoveNotationTests
{
    private static readonly Position MultiJump = Position.Parse("B:W10,19:B6");

    [Theory]
    [InlineData("11-15", false, new[] { 11, 15 })]
    [InlineData(" 6x24 ", true, new[] { 6, 24 })]
    [InlineData("6x15x24", true, new[] { 6, 15, 24 })]
    public void Parse_ValidMove_ReadsSquaresAndKind(string text, bool isCapture, int[] squares)
    {
        var notation = MoveNotation.Parse(text);

        Assert.Equal(isCapture, notation.IsCapture);
        Assert.Equal(squares, notation.Squares.Select(square => square.Number));
        Assert.Equal(text.Trim(), notation.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("11")]
    [InlineData("11-")]
    [InlineData("11-15-19")]
    [InlineData("6x15-24")]
    [InlineData("11 - 15")]
    [InlineData("11X15")]
    [InlineData("a-b")]
    [InlineData("0-4")]
    [InlineData("32-33")]
    [InlineData("99999999999x1")]
    public void Parse_Malformed_ThrowsInvalidPdnAndTryParseFails(string text)
    {
        Assert.Throws<InvalidPdnException>(() => MoveNotation.Parse(text));
        Assert.False(MoveNotation.TryParse(text, out var notation));
        Assert.Null(notation);
    }

    [Fact]
    public void TryParse_Null_Fails() => Assert.False(MoveNotation.TryParse(null, out _));

    [Theory]
    [InlineData("6x24", true)]
    [InlineData("6x15x24", true)]
    [InlineData("6x15", false)]
    [InlineData("6-24", false)]
    [InlineData("6x19x24", false)]
    [InlineData("6x15x24x31", false)]
    public void Matches_ShortAndFullCaptureForms(string text, bool matches) =>
        Assert.Equal(matches, MoveNotation.Parse(text).Matches(MultiJump.LegalMoves[0]));

    [Fact]
    public void FindLegalMove_LegalMove_ReturnsIt()
    {
        var move = Position.Initial.FindLegalMove(MoveNotation.Parse("11-15"));

        Assert.NotNull(move);
        Assert.Equal("11-15", move.ToString());
    }

    [Fact]
    public void FindLegalMove_ShortCaptureForm_ResolvesTheFullPath() =>
        Assert.Equal("6x15x24", MultiJump.FindLegalMove(MoveNotation.Parse("6x24"))?.ToString());

    [Theory]
    [InlineData("B:W21-32:B1-12", "22-18")]
    [InlineData("B:W21-32:B1-12", "9-18")]
    [InlineData("B:W21-32:B1-12", "11x18")]
    [InlineData("B:W18,19,22,25,27,28,30,32:B1,5,6,7,10,12,14,16", "10-15")]
    public void FindLegalMove_IllegalMove_ReturnsNull(string pdn, string move) =>
        Assert.Null(Position.Parse(pdn).FindLegalMove(MoveNotation.Parse(move)));
}
