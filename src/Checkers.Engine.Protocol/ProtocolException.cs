namespace Checkers.Engine.Protocol;

/// <summary>The peer sent a line that is not a valid protocol message.</summary>
public sealed class ProtocolException(string message, Exception? innerException = null)
    : Exception(message, innerException);
