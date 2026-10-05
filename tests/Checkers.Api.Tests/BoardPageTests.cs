using System.Net;

namespace Checkers.Api.Tests;

public sealed class BoardPageTests : IAsyncDisposable
{
    private readonly CheckersApiFactory _factory = new();

    [Theory]
    [InlineData("/", "text/html")]
    [InlineData("/board.js", "text/javascript")]
    [InlineData("/board.css", "text/css")]
    public async Task Get_BoardPageFile_IsServed(string url, string mediaType)
    {
        var response = await _factory.CreateClient().GetAsync(url, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(mediaType, response.Content.Headers.ContentType?.MediaType);
    }

    public ValueTask DisposeAsync() => _factory.DisposeAsync();
}
