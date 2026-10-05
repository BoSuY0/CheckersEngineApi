namespace Checkers.Application.Moves;

/// <summary>The side to move has no legal move, so there is nothing to suggest.</summary>
public sealed class NoLegalMovesException(string position)
    : Exception($"The side to move has no legal move in '{position}'.");
