using HomeMesh.Abstractions.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HomeMesh.Protocol.ZeroTier;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddZeroTierProvider(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ZeroTierOptions>(configuration.GetSection("Providers:ZeroTier"));
        services.AddHttpClient<ZeroTierLocalApiClient>();
        services.AddSingleton<ZeroTierControllerProvider>();
        services.AddSingleton<ISdwanControllerProvider>(sp => sp.GetRequiredService<ZeroTierControllerProvider>());
        return services;
    }
}
