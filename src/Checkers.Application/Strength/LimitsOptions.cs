using System.ComponentModel.DataAnnotations;

namespace Checkers.Application.Strength;

public sealed class LimitsOptions
{
    public const string SectionName = "Limits";

    /// <summary>Search time when the request names no level.</summary>
    [Range(1, int.MaxValue)]
    public int DefaultSoftTimeMs { get; init; }

    /// <summary>Request deadline when the request gives no hardTimeMs.</summary>
    [Range(1, int.MaxValue)]
    public int DefaultHardTimeMs { get; init; }
}
