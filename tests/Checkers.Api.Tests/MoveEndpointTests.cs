using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace Checkers.Api.Tests;

public sealed class MoveEndpointTests : IAsyncDisposable
{
    private const string SpecPosition = "B:W18,19,22,25,27,28,30,32:B1,5,6,7,10,12,14,16";

    private readonly CheckersApiFactory _factory = new();

    [Fact]
    public async Task Suggest_SpecRequest_ReturnsSpecResponseShape()
    {
        var response = await PostAsync("/v1/move/suggest", new
        {
            gameId = "checkers-8x8",
            state = new { notation = "PDN", position = SpecPosition },
            level = "weak",
            limits = new { maxDepth = 12, softTimeMs = 250, hardTimeMs = 1200 },
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.ReadJsonAsync();
        Assert.Equal(
            ["engine", "bestMove", "pv", "scoreOrWDL", "depth", "nodes", "positionKey", "info"],
            body.EnumerateObject().Select(property => property.Name));
        Assert.Equal("chinook", body.GetProperty("engine").GetString());
        Assert.Equal("16x23", body.GetProperty("bestMove").GetString());
        Assert.Equal(["16x23"], body.GetProperty("pv").EnumerateArray().Select(move => move.GetString()));
        Assert.Equal(35, body.GetProperty("scoreOrWDL").GetInt32());
        Assert.Equal(12, body.GetProperty("depth").GetInt32());
        Assert.Equal(4096, body.GetProperty("nodes").GetInt64());
        Assert.Equal($"pdn:{SpecPosition}", body.GetProperty("positionKey").GetString());
        var info = body.GetProperty("info");
        Assert.Equal(["tablebaseHit", "timeMs"], info.EnumerateObject().Select(property => property.Name));
        Assert.False(info.GetProperty("tablebaseHit").GetBoolean());
        Assert.True(info.GetProperty("timeMs").GetInt64() >= 0);
    }

    [Theory]
    [InlineData("not a position")]
    [InlineData("B:W18,19:B1,33")]
    [InlineData("B:W18:B18")]
    public async Task Suggest_InvalidPdn_Returns422(string position)
    {
        var response = await SuggestAsync(position);

        await AssertProblemAsync(HttpStatusCode.UnprocessableEntity, response);
    }

    [Fact]
    public async Task Suggest_SideToMoveHasNoLegalMove_Returns422()
    {
        var response = await SuggestAsync("B:W18:B");

        await AssertProblemAsync(HttpStatusCode.UnprocessableEntity, response);
    }

    [Fact]
    public async Task Suggest_EngineSlowerThanHardTime_Returns504()
    {
        _factory.Pool.SearchDelay = Timeout.InfiniteTimeSpan;

        var response = await PostAsync("/v1/move/suggest", new
        {
            gameId = "checkers-8x8",
            state = new { notation = "PDN", position = SpecPosition },
            limits = new { hardTimeMs = 50 },
        });

        await AssertProblemAsync(HttpStatusCode.GatewayTimeout, response);
    }

    [Fact]
    public async Task Suggest_Answered_LogsRequestWithSearchFields()
    {
        await SuggestAsync(SpecPosition);

        var log = SingleRequestLog();
        Assert.False(string.IsNullOrEmpty(Assert.IsType<string>(log["requestId"])));
        Assert.IsType<long>(log["timeMs"]);
        Assert.Equal(12, log["depth"]);
        Assert.Equal(4096L, log["nodes"]);
        Assert.Equal(false, log["tablebaseHit"]);
    }

    [Fact]
    public async Task Suggest_HardTimeout_LogsRequestWithoutSearchFields()
    {
        _factory.Pool.SearchDelay = Timeout.InfiniteTimeSpan;

        await PostAsync("/v1/move/suggest", new
        {
            gameId = "checkers-8x8",
            state = new { notation = "PDN", position = SpecPosition },
            limits = new { hardTimeMs = 50 },
        });

        var log = SingleRequestLog();
        Assert.False(string.IsNullOrEmpty(Assert.IsType<string>(log["requestId"])));
        Assert.IsType<long>(log["timeMs"]);
        Assert.Null(log["depth"]);
        Assert.Null(log["nodes"]);
        Assert.Null(log["tablebaseHit"]);
    }

    [Theory]
    [InlineData("""{"gameId":"checkers-10x10","state":{"notation":"PDN","position":"B:W18:B14"}}""")]
    [InlineData("""{"gameId":"checkers-8x8","state":{"notation":"FEN","position":"B:W18:B14"}}""")]
    [InlineData("""{"gameId":"checkers-8x8","state":{"notation":"PDN","position":"B:W18:B14"},"level":"ultra"}""")]
    [InlineData("""{"gameId":"checkers-8x8","state":{"notation":"PDN","position":"B:W18:B14"},"limits":{"hardTimeMs":0}}""")]
    public async Task Suggest_MalformedRequest_Returns400(string json)
    {
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _factory.CreateClient().PostAsync("/v1/move/suggest", content, TestContext.Current.CancellationToken);

        await AssertProblemAsync(HttpStatusCode.BadRequest, response);
    }

    [Theory]
    [InlineData("16x23", true)]
    [InlineData("14x23", true)]
    [InlineData("10-15", false)]
    public async Task Validate_WellFormedMove_ReturnsLegality(string move, bool legal)
    {
        var response = await PostAsync("/v1/move/validate", new { position = SpecPosition, move });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal($$"""{"legal":{{(legal ? "true" : "false")}}}""", await response.ReadCompactJsonAsync());
    }

    [Theory]
    [InlineData(SpecPosition, "16 to 23")]
    [InlineData("B:W18:B33", "16x23")]
    public async Task Validate_MalformedPositionOrMove_Returns422(string position, string move)
    {
        var response = await PostAsync("/v1/move/validate", new { position, move });

        await AssertProblemAsync(HttpStatusCode.UnprocessableEntity, response);
    }

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    private static async Task AssertProblemAsync(HttpStatusCode expected, HttpResponseMessage response)
    {
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal((int)expected, (await response.ReadJsonAsync()).GetProperty("status").GetInt32());
    }

    private IReadOnlyDictionary<string, object?> SingleRequestLog() =>
        Assert.Single(_factory.Log.Entries, entry => entry.EventId.Name == "SuggestRequestCompleted").State;

    private Task<HttpResponseMessage> SuggestAsync(string position) =>
        PostAsync("/v1/move/suggest", new
        {
            gameId = "checkers-8x8",
            state = new { notation = "PDN", position },
        });

    private Task<HttpResponseMessage> PostAsync(string url, object body) =>
        _factory.CreateClient().PostAsJsonAsync(url, body, TestContext.Current.CancellationToken);
}
