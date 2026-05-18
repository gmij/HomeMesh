using HomeMesh.Abstractions.Providers;
using HomeMesh.Application.Auth;
using HomeMesh.Application.Members;
using HomeMesh.Application.NetworkConfig;
using HomeMesh.Application.Networks;
using HomeMesh.Application.Providers;
using HomeMesh.Application.Setup;
using HomeMesh.Application.Sync;
using Microsoft.Extensions.DependencyInjection;

namespace HomeMesh.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHomeMeshApplication(this IServiceCollection services)
    {
        services.AddScoped<SetupService>();
        services.AddScoped<AuthService>();
        services.AddScoped<NetworkService>();
        services.AddScoped<RouteService>();
        services.AddScoped<IpPoolService>();
        services.AddScoped<DnsConfigService>();
        services.AddScoped<EasySetupService>();
        services.AddScoped<MemberService>();
        services.AddScoped<NetworkSyncService>();
        services.AddScoped<NetworkConfigSyncService>();
        services.AddSingleton<DemoControllerProvider>();
        services.AddSingleton<ISdwanControllerProvider, DemoControllerProvider>();
        return services;
    }
}
