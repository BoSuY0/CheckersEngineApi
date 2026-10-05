using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Checkers.Engine.Protocol;

/// <summary>Reads one JSON message per line.</summary>
public sealed class JsonLinesReader<TMessage>
    where TMessage : class
{
    private readonly TextReader _reader;
    private readonly JsonTypeInfo<TMessage> _typeInfo;

    internal JsonLinesReader(TextReader reader, JsonTypeInfo<TMessage> typeInfo)
    {
        _reader = reader;
        _typeInfo = typeInfo;
    }

    /// <returns>The next message, or <see langword="null"/> when the stream has ended.</returns>
    /// <exception cref="ProtocolException">The line is not a valid message.</exception>
    public async ValueTask<TMessage?> ReadAsync(CancellationToken cancellationToken)
    {
        var line = await _reader.ReadLineAsync(cancellationToken);
        if (line is null)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize(line, _typeInfo)
                ?? throw new ProtocolException($"Empty message: '{line}'.");
        }
        catch (JsonException exception)
        {
            throw new ProtocolException($"Malformed message: '{line}'.", exception);
        }
    }
}
