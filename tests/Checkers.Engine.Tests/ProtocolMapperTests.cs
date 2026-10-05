using Checkers.Application.Ports;
using Checkers.Domain;
using Checkers.Engine.Protocol;

namespace Checkers.Engine.Tests;

public sealed class ProtocolMapperTests
{
    [Fact]
    public void ToBoard_ListsEveryPieceTypeAndTheSideToMove()
    {
        var board = Position.Parse("W:W18,K22,30:B1,K5,K9").ToBoard();

        Assert.Equivalent(new Board(Side.White, [1], [5, 9], [18, 30], [22]), board, strict: true);
    }

    [Fact]
    public void ToSearchRequest_RoundsTheMoveTimeUpAndKeepsTheDepthCap()
    {
        var request = new SearchLimits(TimeSpan.FromMilliseconds(250.2), 12).ToSearchRequest();

        Assert.Equal(new SearchRequest(251, 12), request);
    }

    [Fact]
    public void ToSearchResult_KeepsEveryField()
    {
        var response = new SearchResponse("9-14", ["9-14", "22-17"], -35, Depth: 12, Nodes: 153201, TablebaseHit: true);

        var result = response.ToSearchResult();

        var expected = new SearchResult("9-14", ["9-14", "22-17"], -35, Nodes: 153201, Depth: 12, TablebaseHit: true);
        Assert.Equivalent(expected, result, strict: true);
    }

    [Theory]
    [InlineData(TablebaseValue.Unknown, Wdl.Unknown)]
    [InlineData(TablebaseValue.Win, Wdl.Win)]
    [InlineData(TablebaseValue.Loss, Wdl.Loss)]
    [InlineData(TablebaseValue.Draw, Wdl.Draw)]
    public void ToWdl_MapsEveryTablebaseValue(TablebaseValue value, Wdl expected) =>
        Assert.Equal(expected, value.ToWdl());
}
