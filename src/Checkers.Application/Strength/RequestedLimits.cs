using System.ComponentModel.DataAnnotations;

namespace Checkers.Application.Strength;

/// <summary>Caller-supplied upper bounds; each one is optional.</summary>
public sealed record RequestedLimits(
    [Range(1, int.MaxValue)] int? MaxDepth,
    [Range(1, int.MaxValue)] int? SoftTimeMs,
    [Range(1, int.MaxValue)] int? HardTimeMs);
