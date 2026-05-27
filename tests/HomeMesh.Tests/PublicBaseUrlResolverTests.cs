using HomeMesh.WebApi;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace HomeMesh.Tests;

public sealed class PublicBaseUrlResolverTests
{
    [Fact]
    public void Resolve_Should_Use_Configured_Public_Base_Url()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("internal-service:8080");

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PublicBaseUrl"] = "https://hm.gmij.win/admin"
            })
            .Build();

        var result = PublicBaseUrlResolver.Resolve(context.Request, configuration);

        Assert.Equal("https://hm.gmij.win", result);
    }

    [Fact]
    public void Resolve_Should_Fall_Back_To_Request_Scheme_And_Host()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("hm.gmij.win");

        var configuration = new ConfigurationBuilder().Build();

        var result = PublicBaseUrlResolver.Resolve(context.Request, configuration);

        Assert.Equal("https://hm.gmij.win", result);
    }

    [Fact]
    public void Resolve_Should_Ignore_Invalid_Configured_Public_Base_Url()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("hm.gmij.win");

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PublicBaseUrl"] = "not-a-valid-url"
            })
            .Build();

        var result = PublicBaseUrlResolver.Resolve(context.Request, configuration);

        Assert.Equal("https://hm.gmij.win", result);
    }
}
