using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Checkers.Engine.Protocol;

/// <summary>Writes one JSON message per line and flushes it immediately.</summary>
public sealed class JsonLinesWriter<TMessage>
    where TMessage : class
{
    private readonly TextWriter _writer;
    private readonly JsonTypeInfo<TMessage> _typeInfo;

    internal JsonLinesWriter(TextWriter writer, JsonTypeInfo<TMessage> typeInfo)
    {
        _writer = writer;
        _typeInfo = typeInfo;
    }

    public async ValueTask WriteAsync(TMessage message, CancellationToken cancellationToken)
    {
        var line = JsonSerializer.Serialize(message, _typeInfo);
        await _writer.WriteLineAsync(line.AsMemory(), cancellationToken);
        await _writer.FlushAsync(cancellationToken);
    }
}
