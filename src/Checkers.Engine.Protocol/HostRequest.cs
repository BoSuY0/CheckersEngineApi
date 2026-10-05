using System.Text.Json.Serialization;

namespace Checkers.Engine.Protocol;

/// <summary>A message sent by the API process to a host process. Each request except <see cref="StopRequest"/> gets exactly one response.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(SetPositionRequest), "setPosition")]
[JsonDerivedType(typeof(SearchRequest), "search")]
[JsonDerivedType(typeof(ProbeRequest), "probe")]
[JsonDerivedType(typeof(StopRequest), "stop")]
public abstract record HostRequest;

/// <summary>Replaces the engine position. Answered by <see cref="PositionSetResponse"/>.</summary>
public sealed record SetPositionRequest(Board Board) : HostRequest;

/// <summary>Searches the current position. Answered by <see cref="SearchResponse"/>.</summary>
/// <param name="MoveTimeMs">Exact search time budget.</param>
/// <param name="MaxDepth">Best-effort nominal depth cap; <see langword="null"/> means no cap.</param>
public sealed record SearchRequest(int MoveTimeMs, int? MaxDepth) : HostRequest;

/// <summary>Looks up capture-free positions in the endgame database. Answered by <see cref="ProbeResponse"/>.</summary>
public sealed record ProbeRequest(IReadOnlyList<Board> Boards) : HostRequest;

/// <summary>Asks a running search to return its best move now. Has no response of its own and is ignored when idle.</summary>
public sealed record StopRequest : HostRequest;
