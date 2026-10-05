using Checkers.Application.Ports;
using Checkers.Application.Strength;

namespace Checkers.Application.Tests.Strength;

public sealed class StrengthPolicyTests
{
    private readonly StrengthPolicy _policy = TestOptions.CreatePolicy();

    [Theory]
    [InlineData(StrengthLevel.Weak, 100, 8)]
    [InlineData(StrengthLevel.Medium, 250, 12)]
    [InlineData(StrengthLevel.Strong, 500, 18)]
    public void ResolveSearchLimits_Level_UsesItsBudget(StrengthLevel level, int moveTimeMs, int maxDepth) =>
        Assert.Equal(Limits(moveTimeMs, maxDepth), _policy.ResolveSearchLimits(level, null));

    [Fact]
    public void ResolveSearchLimits_NoLevel_UsesDefaultSoftTimeWithoutDepthCap() =>
        Assert.Equal(Limits(TestOptions.DefaultSoftTimeMs, null), _policy.ResolveSearchLimits(null, null));

    [Fact]
    public void ResolveSearchLimits_LowerRequestedLimits_CapTheBudget() =>
        Assert.Equal(
            Limits(200, 10),
            _policy.ResolveSearchLimits(StrengthLevel.Strong, new RequestedLimits(MaxDepth: 10, SoftTimeMs: 200, HardTimeMs: null)));

    [Fact]
    public void ResolveSearchLimits_HigherRequestedLimits_DoNotRaiseTheBudget() =>
        Assert.Equal(
            Limits(100, 8),
            _policy.ResolveSearchLimits(StrengthLevel.Weak, new RequestedLimits(MaxDepth: 20, SoftTimeMs: 1000, HardTimeMs: null)));

    [Fact]
    public void ResolveSearchLimits_NoLevel_RequestedDepthBecomesTheCap() =>
        Assert.Equal(
            Limits(TestOptions.DefaultSoftTimeMs, 6),
            _policy.ResolveSearchLimits(null, new RequestedLimits(MaxDepth: 6, SoftTimeMs: null, HardTimeMs: null)));

    [Fact]
    public void ResolveHardTimeout_Requested_UsesIt() =>
        Assert.Equal(
            TimeSpan.FromMilliseconds(800),
            _policy.ResolveHardTimeout(new RequestedLimits(MaxDepth: null, SoftTimeMs: null, HardTimeMs: 800)));

    [Fact]
    public void ResolveHardTimeout_NotRequested_UsesDefault() =>
        Assert.Equal(TimeSpan.FromMilliseconds(TestOptions.DefaultHardTimeMs), _policy.ResolveHardTimeout(null));

    private static SearchLimits Limits(int moveTimeMs, int? maxDepth) =>
        new(TimeSpan.FromMilliseconds(moveTimeMs), maxDepth);
}
