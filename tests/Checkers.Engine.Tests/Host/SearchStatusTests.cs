using Checkers.Engine.KingsRowHost;
using Checkers.Engine.Protocol;

namespace Checkers.Engine.Tests.Host;

/// <summary>Status lines as KingsRow 1.20 writes them.</summary>
public sealed class SearchStatusTests
{
    [Fact]
    public void TryParse_EvaluatedSearch_ReadsValueDepthSpeedAndPv()
    {
        Assert.True(SearchStatus.TryParse("value=156,  depth 21/19.7/35,  0.2s,  10780 kN/s,  pv 14x23 27x18 16x23 25-21", out var status));

        Assert.Equal(156, status.Value);
        Assert.Equal(21, status.Depth);
        Assert.Equal(10780, status.KiloNodesPerSecond);
        Assert.Equal(["14x23", "27x18", "16x23", "25-21"], status.Pv);
        Assert.False(status.IsRootDatabaseDraw);
    }

    [Fact]
    public void TryParse_BoundValue_ReadsTheNegativeValue()
    {
        Assert.True(SearchStatus.TryParse("value<-2692,  depth 16/12.5/22,  0.0s,  5728 kN/s,  pv 30-26 14-9", out var status));

        Assert.Equal(-2692, status.Value);
        Assert.Equal(["30-26", "14-9"], status.Pv);
    }

    [Fact]
    public void TryParse_AbortedSearch_HasAnEmptyPv()
    {
        Assert.True(SearchStatus.TryParse("value<6,  depth 9/8.2/16,  0.0s,  5902 kN/s,  ", out var status));

        Assert.Equal(9, status.Depth);
        Assert.Empty(status.Pv);
    }

    [Fact]
    public void TryParse_RootDatabaseDraw_IsADrawWithoutPv()
    {
        Assert.True(SearchStatus.TryParse("depth 6; 22-18* (0.417), 22-17 (0.694), 26-30 (0.833), ", out var status));

        Assert.True(status.IsRootDatabaseDraw);
        Assert.Equal(6, status.Depth);
        Assert.Equal(0, status.Value);
        Assert.Empty(status.Pv);
    }

    [Theory]
    [InlineData("Thinking...")]
    [InlineData("No moves")]
    [InlineData("Wait for db init; 6 pieces")]
    [InlineData("")]
    public void TryParse_OtherText_IsNoResult(string text) =>
        Assert.False(SearchStatus.TryParse(text, out _));

    [Theory]
    [InlineData(Side.Black, 156)]
    [InlineData(Side.White, -156)]
    public void ScoreFor_TurnsBlacksValueToTheSideToMove(Side side, int expected) =>
        Assert.Equal(expected, Parse("value=156,  depth 21/19.7/35,  0.2s,  10780 kN/s,  pv 14x23").ScoreFor(side));

    [Fact]
    public void NodesIn_MultipliesSpeedByElapsedTime() =>
        Assert.Equal(
            2_156_000,
            Parse("value=156,  depth 21/19.7/35,  0.2s,  10780 kN/s,  pv 14x23").NodesIn(TimeSpan.FromSeconds(0.2)));

    [Theory]
    [InlineData("depth 6; 7-2* (1.000), 7-10* (1.000), ", false, true)]
    [InlineData("value=1,  depth 5/1.0/1,  0.0s,  6 kN/s,  pv 2x9", true, true)]
    [InlineData("value=1,  depth 5/1.0/1,  0.0s,  6 kN/s,  pv 2x9", false, false)]
    [InlineData("value=-2794,  depth 23/20.6/32,  0.2s,  6890 kN/s,  pv 30-26", true, true)]
    [InlineData("value=-2794,  depth 23/20.6/32,  0.2s,  6890 kN/s,  pv 30-26", false, false)]
    [InlineData("value=156,  depth 21/19.7/35,  0.2s,  10780 kN/s,  pv 14x23", true, false)]
    public void IsTablebaseHit_RecognisesDatabaseResults(string text, bool positionInDatabase, bool expected) =>
        Assert.Equal(expected, Parse(text).IsTablebaseHit(positionInDatabase));

    private static SearchStatus Parse(string text)
    {
        Assert.True(SearchStatus.TryParse(text, out var status));
        return status;
    }
}
