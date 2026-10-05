using Checkers.Application.Ports;

namespace Checkers.Application.Moves;

/// <summary>
/// Everything that determines a suggestion: the canonical PDN position and the effective search limits.
/// The hard timeout is left out because it decides only whether an answer arrives, not which one.
/// </summary>
internal readonly record struct SuggestionKey(string Position, SearchLimits Limits);
