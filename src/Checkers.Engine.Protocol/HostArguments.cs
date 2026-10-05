namespace Checkers.Engine.Protocol;

/// <summary>The host's command line: the KingsRow install directory and the endgame database directory (both Windows paths).</summary>
public sealed record HostArguments(string EngineDirectory, string DatabaseDirectory)
{
    public IReadOnlyList<string> ToArgs() => [EngineDirectory, DatabaseDirectory];

    /// <exception cref="ArgumentException">The argument count is not two.</exception>
    public static HostArguments Parse(IReadOnlyList<string> args) =>
        args.Count == 2
            ? new HostArguments(args[0], args[1])
            : throw new ArgumentException("Expected arguments: <engineDirectory> <databaseDirectory>.", nameof(args));
}
