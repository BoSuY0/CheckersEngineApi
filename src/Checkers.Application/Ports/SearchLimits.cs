namespace Checkers.Application.Ports;

/// <param name="MoveTime">Search time; the engine stops when it elapses (the effective softTimeMs).</param>
/// <param name="MaxDepth">Best-effort nominal depth cap; <see langword="null"/> means no cap.</param>
public readonly record struct SearchLimits(TimeSpan MoveTime, int? MaxDepth);
