namespace Checkers.Api.Logging;

internal static partial class RequestLog
{
    // The placeholder names become the JSON state properties, so they use the spec's field names.
    // The search fields are null when the request ended without a suggestion.
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Suggest request {requestId} took {timeMs} ms (depth {depth}, nodes {nodes}, tablebaseHit {tablebaseHit})")]
    public static partial void SuggestRequestCompleted(
        this ILogger logger,
        string requestId,
        long timeMs,
        int? depth,
        long? nodes,
        bool? tablebaseHit);
}
