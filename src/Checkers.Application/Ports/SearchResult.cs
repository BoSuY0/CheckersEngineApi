namespace Checkers.Application.Ports;

/// <summary>An engine answer as reported, before the legality check of the suggestion flow.</summary>
/// <param name="BestMove">PDN notation of the move the engine chose.</param>
/// <param name="Pv">PDN notation of the principal variation, starting with the engine's best move.</param>
/// <param name="ScoreOrWdl">Evaluation from the side to move's point of view (about 100 per man).</param>
/// <param name="TablebaseHit">The engine answered from its endgame database.</param>
public sealed record SearchResult(
    string BestMove,
    IReadOnlyList<string> Pv,
    int ScoreOrWdl,
    long Nodes,
    int Depth,
    bool TablebaseHit);
