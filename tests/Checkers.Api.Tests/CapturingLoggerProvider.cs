using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Checkers.Api.Tests;

/// <summary>Keeps the structured state of every log entry, as the JSON formatter would write it.</summary>
internal sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<LogEntry> _entries = new();

    public IReadOnlyList<LogEntry> Entries => [.. _entries];

    public ILogger CreateLogger(string categoryName) => new Logger(this);

    public void Dispose()
    {
    }

    internal sealed record LogEntry(EventId EventId, IReadOnlyDictionary<string, object?> State);

    private sealed class Logger(CapturingLoggerProvider provider) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (state is IReadOnlyList<KeyValuePair<string, object?>> properties)
            {
                provider._entries.Enqueue(new LogEntry(eventId, properties.ToDictionary()));
            }
        }
    }
}
