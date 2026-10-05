using Checkers.Domain;

namespace Checkers.Application.Moves;

/// <summary>A verified suggestion for a position.</summary>
/// <param name="Engine">Name of the engine that produced the suggestion.</param>
/// <param name="Pv">Legal line from the position, starting with <paramref name="BestMove"/>.</param>
/// <param name="ScoreOrWdl">Engine evaluation, or 1/0/-1 (win/draw/loss) when <paramref name="TablebaseHit"/>; side to move's point of view.</param>
/// <param name="PositionKey">"pdn:" followed by the canonical PDN position.</param>
public sealed record MoveSuggestion(
    string Engine,
    Move BestMove,
    IReadOnlyList<Move> Pv,
    int ScoreOrWdl,
    int Depth,
    long Nodes,
    string PositionKey,
    bool TablebaseHit);
