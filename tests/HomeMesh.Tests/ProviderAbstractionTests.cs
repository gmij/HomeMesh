using HomeMesh.Protocol.ZeroTier;
using Microsoft.Extensions.Options;
using Xunit;

namespace HomeMesh.Tests;

public sealed class ProviderAbstractionTests
{
    [Fact]
    public async Task ZeroTierProvider_Should_Return_Disabled_Status_When_Disabled()
    {
        var options = Options.Create(new ZeroTierOptions
        {
            Enabled = false
        });

        using var httpClient = new HttpClient();
        var apiClient = new ZeroTierLocalApiClient(httpClient, options);
        var provider = new ZeroTierControllerProvider(options, apiClient);

        var status = await provider.GetStatusAsync();

        Assert.Equal("ZeroTier", status.ProviderName);
        Assert.Equal("Disabled", status.Status);
    }
}
