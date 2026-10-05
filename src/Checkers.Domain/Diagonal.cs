namespace Checkers.Domain;

/// <summary>One diagonal step from a square: the adjacent square and, when it is on the board, the jump landing beyond it.</summary>
internal readonly record struct Diagonal(Square Adjacent, Square? Landing);
