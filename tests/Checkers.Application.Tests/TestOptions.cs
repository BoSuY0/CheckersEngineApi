using Checkers.Application.Caching;
using Checkers.Application.Strength;
using Microsoft.Extensions.Options;

namespace Checkers.Application.Tests;

/// <summary>The configuration of the spec's appsettings example.</summary>
internal static class TestOptions
{
    public const int DefaultSoftTimeMs = 300;
    public const int DefaultHardTimeMs = 1200;
    public const int CacheTtlMinutes = 15;

    public static StrengthPolicy CreatePolicy() =>
        new(Options.Create(new LimitsOptions { DefaultSoftTimeMs = DefaultSoftTimeMs, DefaultHardTimeMs = DefaultHardTimeMs }));

    public static IOptions<CacheOptions> Cache { get; } =
        Options.Create(new CacheOptions { Capacity = 20000, TtlMinutes = CacheTtlMinutes });
}
