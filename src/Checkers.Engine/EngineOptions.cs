using System.ComponentModel.DataAnnotations;

namespace Checkers.Engine;

/// <summary>The "Engine" configuration section.</summary>
public sealed class EngineOptions
{
    public const string SectionName = "Engine";

    /// <summary>Engine name reported in every suggestion.</summary>
    [Required]
    public string Type { get; init; } = "";

    /// <summary>KingsRow install directory (Windows path) holding egdb64.dll and engines\Kingsrow64.dll.</summary>
    [Required]
    public string Path { get; init; } = "";

    [Range(1, 64)]
    public int Workers { get; init; }

    /// <summary>Chinook endgame database directory (Windows path).</summary>
    /// <remarks>egdb64.dll overruns a stack buffer when the directory plus a database file name exceeds MAX_PATH.</remarks>
    [Required]
    [MaxLength(200)]
    public string Databases { get; init; } = "";

    /// <summary>Worker host executable; a relative path is resolved against the application base directory.</summary>
    [Required]
    public string HostPath { get; init; } = "";

    /// <summary>Program that runs the Windows host, e.g. "wine" on a Linux development machine; empty on Windows.</summary>
    public string Launcher { get; init; } = "";
}
