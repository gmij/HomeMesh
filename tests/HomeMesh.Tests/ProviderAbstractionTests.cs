using HomeMesh.Abstractions.Providers;
using HomeMesh.Protocol.ZeroTier;
using Microsoft.Extensions.Options;

namespace HomeMesh.Tests;

public sealed class ProviderAbstractionTests
{
    [Fact]
    public async Task ZeroTierProvider_Should_Return_Configured_Status()
    {
        var provider = new ZeroTierControllerProvider(Options.Create(new ZeroTierOptions
        {
            Enabled = true,
            ApiBaseUrl = "http://127.0.0.1:9993"
        }));

        ProviderHealthStatus status = await provider.GetStatusAsync();

        Assert.Equal("ZeroTier", status.ProviderName);
        Assert.Equal("Configured", status.Status);
    }
}
