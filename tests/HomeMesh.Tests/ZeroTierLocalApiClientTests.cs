using System.Net;
using System.Net.Http;
using System.Text;
using HomeMesh.Protocol.ZeroTier;
using Microsoft.Extensions.Options;
using Xunit;

namespace HomeMesh.Tests;

public sealed class ZeroTierLocalApiClientTests
{
    [Fact]
    public async Task ListMemberIdsAsync_Should_Handle_Empty_Object_Response()
    {
        var httpClient = CreateHttpClient("{}");
        var tokenPath = CreateTempToken();
        var options = Options.Create(new ZeroTierOptions
        {
            Port = 9993,
            AuthTokenPath = tokenPath
        });
        var client = new ZeroTierLocalApiClient(httpClient, options);

        var result = await client.ListMemberIdsAsync("8056c2e21c000001");

        Assert.NotNull(result);
        Assert.Empty(result);

        File.Delete(tokenPath);
    }

    [Fact]
    public async Task ListMemberIdsAsync_Should_Handle_Array_Response()
    {
        var httpClient = CreateHttpClient("[\"a\",\"b\"]");
        var tokenPath = CreateTempToken();
        var options = Options.Create(new ZeroTierOptions
        {
            Port = 9993,
            AuthTokenPath = tokenPath
        });
        var client = new ZeroTierLocalApiClient(httpClient, options);

        var result = await client.ListMemberIdsAsync("8056c2e21c000001");

        Assert.Equal(2, result.Count);
        Assert.Equal("a", result[0]);
        Assert.Equal("b", result[1]);

        File.Delete(tokenPath);
    }

    private static string CreateTempToken()
    {
        var tokenPath = Path.GetTempFileName();
        File.WriteAllText(tokenPath, "test-token");
        return tokenPath;
    }

    private static HttpClient CreateHttpClient(string json)
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

        return new HttpClient(handler);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(responseFactory(request));
        }
    }
}