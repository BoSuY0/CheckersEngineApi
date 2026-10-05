using Checkers.Application.Moves;
using Checkers.Domain;

namespace Checkers.Application.Tests.Moves;

public sealed class ValidateMoveServiceTests
{
    // White to move with the only legal move 18x9x2.
    private const string DoubleJump = "W:W18,22,25,27,28,30,32:B1,5,6,7,10,12,14,23";

    private readonly ValidateMoveService _service = new();

    [Theory]
    [InlineData(TestPositions.Midgame, "14x23")]
    [InlineData(TestPositions.Midgame, "16x23")]
    [InlineData(DoubleJump, "18x2")]
    [InlineData(DoubleJump, "18x9x2")]
    public void IsLegal_LegalMove_IsTrue(string position, string move) =>
        Assert.True(_service.IsLegal(position, move));

    [Theory]
    [InlineData(TestPositions.Midgame, "10-15")]
    [InlineData(TestPositions.Midgame, "22-17")]
    [InlineData(DoubleJump, "18x9")]
    public void IsLegal_IllegalMove_IsFalse(string position, string move) =>
        Assert.False(_service.IsLegal(position, move));

    [Theory]
    [InlineData(TestPositions.Midgame, "14_23")]
    [InlineData("B:W18:B33", "1-5")]
    public void IsLegal_MalformedInput_ThrowsInvalidPdn(string position, string move) =>
        Assert.Throws<InvalidPdnException>(() => _service.IsLegal(position, move));
}
