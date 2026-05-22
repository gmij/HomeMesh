using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace HomeMesh.Tests;

public sealed class WebApiSmokeTests : IClassFixture<WebApplicationFactory<HomeMesh.WebApi.Program>>
{
    private readonly WebApplicationFactory<HomeMesh.WebApi.Program> _factory;

    public WebApiSmokeTests(WebApplicationFactory<HomeMesh.WebApi.Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_Should_Return_Healthy()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.Equal("Healthy", payload?.Status);
    }

    [Fact]
    public async Task Protected_Api_Should_Return_Unauthorized_When_Not_Logged_In()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/networks");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Auth_Status_Should_Be_Public()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/status");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Root_Should_Return_Html()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        response.EnsureSuccessStatusCode();
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    private sealed record HealthResponse(string Status, string Service, DateTimeOffset CheckedAt);
}
