using Checkers.Application.Moves;
using Checkers.Domain;

namespace Checkers.Application.Tests.Moves;

public sealed class LegalMovesServiceTests
{
    private readonly LegalMovesService _service = new();

    [Fact]
    public void List_Midgame_PairsEachCaptureWithItsResult()
    {
        var moves = _service.List(TestPositions.Midgame);

        Assert.Equal(TestPositions.Midgame, moves.Position.ToString());
        Assert.Equal(
            [
                ("14x23", "W:W19,22,25,27,28,30,32:B1,5,6,7,10,12,16,23"),
                ("16x23", "W:W18,22,25,27,28,30,32:B1,5,6,7,10,12,14,23"),
            ],
            moves.Moves.Select(legal => (legal.Move.ToString(), legal.Result.ToString())));
    }

    [Fact]
    public void List_RangesAndWhitespace_ReturnsCanonicalPosition() =>
        Assert.Equal(
            "B:W21,22,23,24,25,26,27,28,29,30,31,32:B1,2,3,4,5,6,7,8,9,10,11,12",
            _service.List(" B:W21-32 : B1-12 ").Position.ToString());

    [Fact]
    public void List_SideToMoveIsBlocked_HasNoMoves() =>
        Assert.Empty(_service.List("B:W18:B").Moves);

    [Fact]
    public void List_InvalidPdn_ThrowsInvalidPdn() =>
        Assert.Throws<InvalidPdnException>(() => _service.List("B:W18:B33"));
}
