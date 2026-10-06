using Checkers.Domain;

namespace Checkers.Application.Moves;

/// <summary>A legal move and the position it leads to.</summary>
public sealed record LegalMove(Move Move, Position Result);
