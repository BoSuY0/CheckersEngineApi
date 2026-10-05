using Checkers.Engine.Protocol;

namespace Checkers.Engine.KingsRowHost;

internal static class BoardExtensions
{
    public static int PieceCount(this Board board) =>
        board.BlackMen.Count + board.BlackKings.Count + board.WhiteMen.Count + board.WhiteKings.Count;
}
