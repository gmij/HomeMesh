using HomeMesh.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HomeMesh.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHomeMeshInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("HomeMesh")
            ?? "Data Source=homemesh.db";

        services.AddDbContext<HomeMeshDbContext>(options => options.UseSqlite(connectionString));
        return services;
    }
}
