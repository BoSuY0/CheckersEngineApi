using Microsoft.Extensions.Logging;

namespace Checkers.Engine;

internal static partial class EngineLog
{
    [LoggerMessage(Level = LogLevel.Information,
        Message = "Engine host {ProcessId} is ready: {EngineName} with {TablebasePieces}-piece databases")]
    public static partial void HostStarted(ILogger logger, int processId, string engineName, int tablebasePieces);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Engine host {ProcessId}: {Line}")]
    public static partial void HostDiagnostic(ILogger logger, int processId, string line);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Engine worker {Worker} is restarting its host")]
    public static partial void WorkerRestarting(ILogger logger, int worker, Exception reason);

    [LoggerMessage(Level = LogLevel.Error, Message = "Engine worker {Worker} could not start its host")]
    public static partial void WorkerStartFailed(ILogger logger, int worker, Exception exception);
}
