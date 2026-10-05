using System.Runtime.InteropServices;
using Checkers.Engine.Protocol;

namespace Checkers.Engine.KingsRowHost.Native;

/// <summary>
/// <c>EGDB_NORMAL_BITBOARD</c>: bit <c>n - 1</c> stands for PDN square <c>n</c>; <see cref="Kings"/> covers both colours.
/// The native type is a 16-byte union, so the struct is padded to that size.
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 16)]
internal readonly struct EgdbBitboard(uint black, uint white, uint kings)
{
    public readonly uint Black = black;
    public readonly uint White = white;
    public readonly uint Kings = kings;

    public static EgdbBitboard From(Board board) => new(
        Bits(board.BlackMen) | Bits(board.BlackKings),
        Bits(board.WhiteMen) | Bits(board.WhiteKings),
        Bits(board.BlackKings) | Bits(board.WhiteKings));

    private static uint Bits(IReadOnlyList<int> squares)
    {
        var bits = 0u;
        foreach (var square in squares)
        {
            bits |= 1u << (square - 1);
        }

        return bits;
    }
}
