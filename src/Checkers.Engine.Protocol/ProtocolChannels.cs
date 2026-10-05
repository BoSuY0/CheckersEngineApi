using System.Text;

namespace Checkers.Engine.Protocol;

/// <summary>Creates the typed line readers and writers for both ends of a host's standard streams.</summary>
public static class ProtocolChannels
{
    /// <summary>UTF-8 without a byte order mark, so the first line of a stream parses as JSON.</summary>
    public static Encoding Encoding { get; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    public static JsonLinesWriter<HostRequest> RequestWriter(TextWriter writer) =>
        new(writer, ProtocolJsonContext.Default.HostRequest);

    public static JsonLinesReader<HostRequest> RequestReader(TextReader reader) =>
        new(reader, ProtocolJsonContext.Default.HostRequest);

    public static JsonLinesWriter<HostResponse> ResponseWriter(TextWriter writer) =>
        new(writer, ProtocolJsonContext.Default.HostResponse);

    public static JsonLinesReader<HostResponse> ResponseReader(TextReader reader) =>
        new(reader, ProtocolJsonContext.Default.HostResponse);
}
