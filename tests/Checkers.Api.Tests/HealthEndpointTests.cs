using System.Net;

namespace Checkers.Api.Tests;

public sealed class HealthEndpointTests : IAsyncDisposable
{
    private readonly CheckersApiFactory _factory = new();

    [Fact]
    public async Task Get_AllWorkersReady_ReturnsOkWithReadyWorkerCount()
    {
        var response = await _factory.CreateClient().GetAsync("/healthz", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("""{"ok":true,"workers":2}""", await response.ReadCompactJsonAsync());
    }

    [Fact]
    public async Task Get_WorkerNotReady_ReturnsNotOkWithReadyWorkerCount()
    {
        _factory.Pool.ReadyWorkerCount = 1;

        var response = await _factory.CreateClient().GetAsync("/healthz", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("""{"ok":false,"workers":1}""", await response.ReadCompactJsonAsync());
    }

    public ValueTask DisposeAsync() => _factory.DisposeAsync();
}
