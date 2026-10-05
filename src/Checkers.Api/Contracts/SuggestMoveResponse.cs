using System.Text.Json.Serialization;
using Checkers.Application.Moves;

namespace Checkers.Api.Contracts;

public sealed record SuggestMoveResponse(
    string Engine,
    string BestMove,
    IReadOnlyList<string> Pv,
    [property: JsonPropertyName("scoreOrWDL")] int ScoreOrWdl,
    int Depth,
    long Nodes,
    string PositionKey,
    SuggestionInfo Info)
{
    public static SuggestMoveResponse From(MoveSuggestion suggestion, TimeSpan elapsed) => new(
        suggestion.Engine,
        suggestion.BestMove.ToString(),
        [.. suggestion.Pv.Select(move => move.ToString())],
        suggestion.ScoreOrWdl,
        suggestion.Depth,
        suggestion.Nodes,
        suggestion.PositionKey,
        new SuggestionInfo(suggestion.TablebaseHit, (long)elapsed.TotalMilliseconds));
}
