using System.Net.Http.Json;
using System.Text.Json;

namespace Checkers.Api.Tests;

internal static class HttpResponseMessageExtensions
{
    public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

    public static async Task<string> ReadCompactJsonAsync(this HttpResponseMessage response) =>
        JsonSerializer.Serialize(await response.ReadJsonAsync());
}
