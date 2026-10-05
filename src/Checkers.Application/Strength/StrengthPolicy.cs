using Checkers.Application.Ports;
using Microsoft.Extensions.Options;

namespace Checkers.Application.Strength;

/// <summary>Turns a strength level and the caller's limits into effective search limits and a hard deadline.</summary>
public sealed class StrengthPolicy(IOptions<LimitsOptions> options)
{
    private readonly LimitsOptions _options = options.Value;

    /// <summary>
    /// The level's budget (or the configured default time when no level is given), with every
    /// requested limit applied as an upper bound.
    /// </summary>
    public SearchLimits ResolveSearchLimits(StrengthLevel? level, RequestedLimits? requested)
    {
        var budget = Budget(level);
        return new SearchLimits(
            Cap(budget.MoveTime, requested?.SoftTimeMs),
            Cap(budget.MaxDepth, requested?.MaxDepth));
    }

    public TimeSpan ResolveHardTimeout(RequestedLimits? requested) =>
        TimeSpan.FromMilliseconds(requested?.HardTimeMs ?? _options.DefaultHardTimeMs);

    // Each level takes the upper end of the spec's depth range and the spec's move time; strong uses the
    // lower end of 500-600 ms so that a search plus overhead stays under the 600 ms acceptance limit.
    private SearchLimits Budget(StrengthLevel? level) => level switch
    {
        StrengthLevel.Weak => new SearchLimits(TimeSpan.FromMilliseconds(100), 8),
        StrengthLevel.Medium => new SearchLimits(TimeSpan.FromMilliseconds(250), 12),
        StrengthLevel.Strong => new SearchLimits(TimeSpan.FromMilliseconds(500), 18),
        _ => new SearchLimits(TimeSpan.FromMilliseconds(_options.DefaultSoftTimeMs), null),
    };

    private static TimeSpan Cap(TimeSpan budget, int? capMs) =>
        capMs is { } ms && ms < budget.TotalMilliseconds ? TimeSpan.FromMilliseconds(ms) : budget;

    private static int? Cap(int? budget, int? cap) =>
        budget is { } value && cap is { } limit ? Math.Min(value, limit) : budget ?? cap;
}
