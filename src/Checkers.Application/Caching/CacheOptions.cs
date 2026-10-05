using System.ComponentModel.DataAnnotations;

namespace Checkers.Application.Caching;

public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    [Range(1, int.MaxValue)]
    public int Capacity { get; init; }

    [Range(1, int.MaxValue)]
    public int TtlMinutes { get; init; }
}
