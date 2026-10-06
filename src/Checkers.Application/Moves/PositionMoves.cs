using Checkers.Domain;

namespace Checkers.Application.Moves;

/// <summary>A position and every legal move of its side to move; no moves means that side has lost.</summary>
public sealed record PositionMoves(Position Position, IReadOnlyList<LegalMove> Moves);
