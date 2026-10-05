namespace Checkers.Engine.KingsRowHost;

internal static class HostSettings
{
    /// <summary>KingsRow's search hash table in MB: its own default.</summary>
    public const int HashMb = 128;

    /// <summary>KingsRow's endgame database buffers in MB. Its default of 16384 is far too large for several workers on one machine.</summary>
    public const int DatabaseCacheMb = 512;

    /// <summary>The probe driver's cache in MB: enough to keep the whole 2-6 piece database (47 MB) in memory.</summary>
    public const int ProbeCacheMb = 64;

    /// <summary>How often a depth-capped search reads the depth KingsRow reports. KingsRow has no depth limit of its own.</summary>
    public static readonly TimeSpan DepthPollInterval = TimeSpan.FromMilliseconds(5);
}
