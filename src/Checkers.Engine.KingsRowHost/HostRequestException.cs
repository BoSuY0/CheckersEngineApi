namespace Checkers.Engine.KingsRowHost;

/// <summary>A request could not be carried out. It is answered with an error response, and the host stays usable.</summary>
internal sealed class HostRequestException(string message) : Exception(message);
