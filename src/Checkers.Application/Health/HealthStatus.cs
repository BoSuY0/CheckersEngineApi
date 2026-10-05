namespace Checkers.Application.Health;

/// <param name="Ok">Every configured worker is ready.</param>
/// <param name="Workers">Number of ready workers.</param>
public sealed record HealthStatus(bool Ok, int Workers);
