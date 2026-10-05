using System.Numerics;

namespace Checkers.Domain;

/// <summary>
/// An immutable English/American 8x8 checkers position: the pieces and the side to move.
/// Every instance satisfies the position invariants (at most 12 pieces per side, no man on its own crowning row).
/// </summary>
public sealed class Position : IEquatable<Position>
{
    public const int MaxPiecesPerSide = 12;

    // Bit n-1 of each bitboard stands for square n.
    private readonly uint _black;
    private readonly uint _white;
    private readonly uint _kings;

    // Racing threads compute equal lists, so the lazy cache needs no lock.
    private IReadOnlyList<Move>? _legalMoves;

    private Position(uint black, uint white, uint kings, PieceColor sideToMove)
    {
        _black = black;
        _white = white;
        _kings = kings;
        SideToMove = sideToMove;
    }

    /// <summary>The standard starting position, Black to move.</summary>
    public static Position Initial { get; } = Parse("B:W21-32:B1-12");

    public PieceColor SideToMove { get; }

    public int PieceCount => BitOperations.PopCount(_black | _white);

    public Piece? this[Square square]
    {
        get
        {
            var bit = Bit(square);
            var kind = (_kings & bit) == 0 ? PieceKind.Man : PieceKind.King;
            return (_black & bit) != 0 ? new Piece(PieceColor.Black, kind)
                : (_white & bit) != 0 ? new Piece(PieceColor.White, kind)
                : null;
        }
    }

    /// <summary>All legal moves of the side to move; only captures when a capture exists.</summary>
    public IReadOnlyList<Move> LegalMoves => _legalMoves ??= MoveGenerator.Generate(this);

    /// <summary>
    /// Parses a PDN FEN position such as "B:W18,19,K22:B1-3,5" (either colour list first, square ranges allowed).
    /// </summary>
    /// <exception cref="InvalidPdnException">The text is malformed or the position is impossible.</exception>
    public static Position Parse(string pdn) => PdnFen.Parse(pdn);

    /// <summary>True when <paramref name="color"/> would have a capture if it were to move.</summary>
    public bool HasCapture(PieceColor color) => MoveGenerator.HasCapture(this, color);

    /// <exception cref="ArgumentException"><paramref name="move"/> is not one of <see cref="LegalMoves"/>.</exception>
    public Position Apply(Move move)
    {
        ArgumentNullException.ThrowIfNull(move);
        if (!LegalMoves.Contains(move))
        {
            throw new ArgumentException($"{move} is not a legal move in {this}.", nameof(move));
        }

        var from = Bit(move.From);
        var to = Bit(move.To);
        var captured = move.Captured.Aggregate(0u, (mask, square) => mask | Bit(square));
        var endsAsKing = (_kings & from) != 0 || BoardGeometry.IsCrowningSquare(move.To, SideToMove);
        var kings = (_kings & ~from & ~captured) | (endsAsKing ? to : 0u);

        uint Update(uint pieces, PieceColor color) =>
            color == SideToMove ? (pieces & ~from) | to : pieces & ~captured;

        return new Position(Update(_black, PieceColor.Black), Update(_white, PieceColor.White), kings, SideToMove.Opponent());
    }

    /// <returns>The first legal move, in <see cref="LegalMoves"/> order, that the notation matches; otherwise <see langword="null"/>.</returns>
    public Move? FindLegalMove(MoveNotation notation)
    {
        ArgumentNullException.ThrowIfNull(notation);
        return LegalMoves.FirstOrDefault(notation.Matches);
    }

    public bool Equals(Position? other) =>
        other is not null
        && _black == other._black
        && _white == other._white
        && _kings == other._kings
        && SideToMove == other.SideToMove;

    public override bool Equals(object? obj) => Equals(obj as Position);

    public override int GetHashCode() => HashCode.Combine(_black, _white, _kings, SideToMove);

    /// <summary>The canonical PDN FEN: side to move, White list, Black list, squares ascending, kings prefixed by "K", no ranges.</summary>
    public override string ToString() => PdnFen.Format(this);

    /// <summary>Creates a position from pieces on distinct squares, enforcing the position invariants.</summary>
    /// <exception cref="InvalidPdnException">The pieces violate a position invariant.</exception>
    internal static Position Create(PieceColor sideToMove, IReadOnlyDictionary<Square, Piece> pieces)
    {
        foreach (var color in Enum.GetValues<PieceColor>())
        {
            if (pieces.Values.Count(piece => piece.Color == color) > MaxPiecesPerSide)
            {
                throw new InvalidPdnException($"{color} has more than {MaxPiecesPerSide} pieces.");
            }
        }

        var black = 0u;
        var white = 0u;
        var kings = 0u;
        foreach (var (square, piece) in pieces)
        {
            // Such a man would already have been crowned.
            if (piece.Kind == PieceKind.Man && BoardGeometry.IsCrowningSquare(square, piece.Color))
            {
                throw new InvalidPdnException($"The {piece.Color} man on square {square} stands on its crowning row.");
            }

            var bit = Bit(square);
            if (piece.Color == PieceColor.Black)
            {
                black |= bit;
            }
            else
            {
                white |= bit;
            }

            if (piece.Kind == PieceKind.King)
            {
                kings |= bit;
            }
        }

        return new Position(black, white, kings, sideToMove);
    }

    private static uint Bit(Square square) => 1u << (square.Number - 1);
}
