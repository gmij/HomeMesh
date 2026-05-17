using HomeMesh.Abstractions.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HomeMesh.Protocol.ZeroTier;

public static class DependencyInjection
{
    public static IServiceCollection AddZeroTierProvider(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ZeroTierOptions>(configuration.GetSection("Providers:ZeroTier"));
        services.AddSingleton<ISdwanControllerProvider, ZeroTierControllerProvider>();
        return services;
    }
}
