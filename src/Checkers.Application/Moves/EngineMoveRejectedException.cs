namespace Checkers.Application.Moves;

/// <summary>Neither the engine's best move nor any move of its principal variation is legal in the position.</summary>
public sealed class EngineMoveRejectedException(string position, IEnumerable<string> candidates)
    : Exception($"The engine returned no legal move for '{position}' (candidates: {string.Join(", ", candidates)}).");
