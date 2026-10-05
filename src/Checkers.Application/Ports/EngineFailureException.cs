namespace Checkers.Application.Ports;

/// <summary>An engine worker failed to start, broke down, or reported an error instead of an answer.</summary>
public sealed class EngineFailureException(string message, Exception? innerException = null)
    : Exception(message, innerException);
