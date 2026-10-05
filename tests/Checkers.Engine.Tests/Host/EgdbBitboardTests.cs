using Checkers.Engine.KingsRowHost.Native;
using Checkers.Engine.Protocol;

namespace Checkers.Engine.Tests.Host;

public sealed class EgdbBitboardTests
{
    [Fact]
    public void From_SetsBitNMinusOneForSquareN_WithKingsOfBothColours()
    {
        var bitboard = EgdbBitboard.From(new Board(Side.Black, [1], [5], [28], [32]));

        Assert.Equal((1u << 0) | (1u << 4), bitboard.Black);
        Assert.Equal((1u << 27) | (1u << 31), bitboard.White);
        Assert.Equal((1u << 4) | (1u << 31), bitboard.Kings);
    }
}
