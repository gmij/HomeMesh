using HomeMesh.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HomeMesh.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddHomeMeshInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("HomeMesh") ?? "Data Source=data/homemesh.db";

        services.AddDbContext<HomeMeshDbContext>(options =>
        {
            options.UseSqlite(connectionString);
        });

        return services;
    }
}
