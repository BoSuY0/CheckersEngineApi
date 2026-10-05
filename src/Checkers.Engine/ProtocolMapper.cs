using Checkers.Application.Ports;
using Checkers.Domain;
using Checkers.Engine.Protocol;

namespace Checkers.Engine;

/// <summary>The single mapping between the domain and application types and the host protocol.</summary>
internal static class ProtocolMapper
{
    public static Board ToBoard(this Position position)
    {
        int[] SquaresOf(PieceColor color, PieceKind kind) =>
        [
            .. Square.All
                .Where(square => position[square] == new Piece(color, kind))
                .Select(square => square.Number),
        ];

        return new Board(
            position.SideToMove.ToSide(),
            SquaresOf(PieceColor.Black, PieceKind.Man),
            SquaresOf(PieceColor.Black, PieceKind.King),
            SquaresOf(PieceColor.White, PieceKind.Man),
            SquaresOf(PieceColor.White, PieceKind.King));
    }

    /// <remarks>The move time is rounded up to whole milliseconds.</remarks>
    public static SearchRequest ToSearchRequest(this SearchLimits limits) =>
        new(checked((int)Math.Ceiling(limits.MoveTime.TotalMilliseconds)), limits.MaxDepth);

    public static SearchResult ToSearchResult(this SearchResponse response) =>
        new(response.BestMove, response.Pv, response.Score, response.Nodes, response.Depth, response.TablebaseHit);

    public static Wdl ToWdl(this TablebaseValue value) => value switch
    {
        TablebaseValue.Unknown => Wdl.Unknown,
        TablebaseValue.Win => Wdl.Win,
        TablebaseValue.Loss => Wdl.Loss,
        TablebaseValue.Draw => Wdl.Draw,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    private static Side ToSide(this PieceColor color) => color switch
    {
        PieceColor.Black => Side.Black,
        PieceColor.White => Side.White,
        _ => throw new ArgumentOutOfRangeException(nameof(color), color, null),
    };
}
