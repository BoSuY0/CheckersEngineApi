using System.Text.Json.Serialization;

namespace Checkers.Engine.Protocol;

/// <summary>A message sent by a host process to the API process.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ReadyResponse), "ready")]
[JsonDerivedType(typeof(PositionSetResponse), "positionSet")]
[JsonDerivedType(typeof(SearchResponse), "searchResult")]
[JsonDerivedType(typeof(ProbeResponse), "probeResult")]
[JsonDerivedType(typeof(ErrorResponse), "error")]
public abstract record HostResponse;

/// <summary>Sent once, unsolicited, after the engine and the endgame database are loaded.</summary>
public sealed record ReadyResponse(string EngineName, int TablebasePieces) : HostResponse;

public sealed record PositionSetResponse : HostResponse;

/// <param name="BestMove">The move the engine played, in PDN notation.</param>
/// <param name="Pv">The principal variation as reported by the engine, starting with its best move.</param>
/// <param name="Score">Evaluation from the side to move's point of view (about 100 per man).</param>
/// <param name="Depth">Nominal search depth reached.</param>
/// <param name="Nodes">Nodes searched.</param>
/// <param name="TablebaseHit">The engine reported an endgame database result for the root position.</param>
public sealed record SearchResponse(
    string BestMove,
    IReadOnlyList<string> Pv,
    int Score,
    int Depth,
    long Nodes,
    bool TablebaseHit) : HostResponse;

/// <summary>One value per board of the request, in the same order.</summary>
public sealed record ProbeResponse(IReadOnlyList<TablebaseValue> Values) : HostResponse;

/// <summary>The request failed; the host stays usable.</summary>
public sealed record ErrorResponse(string Message) : HostResponse;
