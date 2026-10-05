using Checkers.Engine.Protocol;

namespace Checkers.Engine.Tests;

/// <summary>The wire format between the API and the host process.</summary>
public sealed class ProtocolRoundTripTests
{
    private static readonly Board SampleBoard = new(Side.Black, [1, 5], [9], [18, 22], [30]);

    public static TheoryData<HostRequest> Requests =>
    [
        new SetPositionRequest(SampleBoard),
        new SearchRequest(250, 12),
        new SearchRequest(250, null),
        new ProbeRequest([SampleBoard, SampleBoard with { ToMove = Side.White }]),
        new StopRequest(),
    ];

    public static TheoryData<HostResponse> Responses =>
    [
        new ReadyResponse("Kingsrow(x64) 1.20", 8),
        new PositionSetResponse(),
        new SearchResponse("6x15x24", ["6x15x24", "28x19"], -120, 14, 153201, false),
        new ProbeResponse([TablebaseValue.Win, TablebaseValue.Loss, TablebaseValue.Draw, TablebaseValue.Unknown]),
        new ErrorResponse("No legal moves."),
    ];

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Theory]
    [MemberData(nameof(Requests), DisableDiscoveryEnumeration = true)]
    public async Task Request_SurvivesARoundTrip(HostRequest request)
    {
        var line = await WriteAsync(ProtocolChannels.RequestWriter, request);

        var copy = await ReadAsync(ProtocolChannels.RequestReader, line);

        Assert.IsType(request.GetType(), copy);
        Assert.Equivalent(request, copy, strict: true);
    }

    [Theory]
    [MemberData(nameof(Responses), DisableDiscoveryEnumeration = true)]
    public async Task Response_SurvivesARoundTrip(HostResponse response)
    {
        var line = await WriteAsync(ProtocolChannels.ResponseWriter, response);

        var copy = await ReadAsync(ProtocolChannels.ResponseReader, line);

        Assert.IsType(response.GetType(), copy);
        Assert.Equivalent(response, copy, strict: true);
    }

    [Fact]
    public async Task Request_IsOneCamelCaseLineWithTheTypeFirstAndNoNulls()
    {
        var line = await WriteAsync(ProtocolChannels.RequestWriter, new SearchRequest(250, null));

        Assert.Equal("{\"type\":\"search\",\"moveTimeMs\":250}" + Environment.NewLine, line);
    }

    [Fact]
    public async Task Response_ReadsEnumNames()
    {
        const string Line = """{"type":"probeResult","values":["Win","Draw"]}""";

        var response = await ReadAsync(ProtocolChannels.ResponseReader, Line);

        Assert.Equal([TablebaseValue.Win, TablebaseValue.Draw], Assert.IsType<ProbeResponse>(response).Values);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{"type":"unknown"}""")]
    [InlineData("null")]
    public async Task MalformedLine_ThrowsProtocolException(string line) =>
        await Assert.ThrowsAsync<ProtocolException>(() => ReadAsync(ProtocolChannels.ResponseReader, line));

    [Fact]
    public async Task EndOfStream_ReadsAsNull() =>
        Assert.Null(await ReadAsync(ProtocolChannels.ResponseReader, ""));

    private static async Task<string> WriteAsync<TMessage>(
        Func<TextWriter, JsonLinesWriter<TMessage>> createWriter,
        TMessage message)
        where TMessage : class
    {
        using var text = new StringWriter();
        await createWriter(text).WriteAsync(message, Token);
        return text.ToString();
    }

    private static async Task<TMessage?> ReadAsync<TMessage>(
        Func<TextReader, JsonLinesReader<TMessage>> createReader,
        string text)
        where TMessage : class
    {
        using var reader = new StringReader(text);
        return await createReader(reader).ReadAsync(Token);
    }
}
