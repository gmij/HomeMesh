using HomeMesh.Domain.Entities;
using HomeMesh.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeMesh.Application.NetworkConfig;

public sealed class RouteService(HomeMeshDbContext db)
{
    public async Task<IReadOnlyList<RouteDto>> ListAsync(string networkId, CancellationToken cancellationToken = default)
    {
        return await db.Routes
            .Where(x => x.NetworkId == networkId)
            .OrderBy(x => x.Target)
            .Select(x => new RouteDto(x.Id, x.NetworkId, x.Type, x.Target, x.Via, x.Enabled, x.ProviderManaged))
            .ToListAsync(cancellationToken);
    }

    public async Task<RouteDto> CreateAsync(string networkId, CreateRouteRequest request, CancellationToken cancellationToken = default)
    {
        var networkExists = await db.Networks.AnyAsync(x => x.Id == networkId, cancellationToken);
        if (!networkExists)
        {
            throw new InvalidOperationException("Network not found.");
        }

        if (string.IsNullOrWhiteSpace(request.Target) || !request.Target.Contains('/'))
        {
            throw new InvalidOperationException("Route target must be a CIDR value, for example 192.168.1.0/24.");
        }

        var now = DateTimeOffset.UtcNow;
        var route = new Route
        {
            Id = IdGenerator.NewId("route"),
            NetworkId = networkId,
            Type = string.IsNullOrWhiteSpace(request.Type) ? "VirtualSubnet" : request.Type.Trim(),
            Target = request.Target.Trim(),
            Via = string.IsNullOrWhiteSpace(request.Via) ? null : request.Via.Trim(),
            Enabled = request.Enabled,
            ProviderManaged = request.ProviderManaged,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Routes.Add(route);
        db.AuditLogs.Add(new AuditLog
        {
            Id = IdGenerator.NewId("audit"),
            Type = "RouteCreated",
            Actor = "system",
            TargetType = "Route",
            TargetId = route.Id,
            Message = $"新增路由：{route.Target}",
            CreatedAt = now
        });

        await db.SaveChangesAsync(cancellationToken);
        return ToDto(route);
    }

    public async Task<bool> DeleteAsync(string networkId, string routeId, CancellationToken cancellationToken = default)
    {
        var route = await db.Routes.FirstOrDefaultAsync(x => x.NetworkId == networkId && x.Id == routeId, cancellationToken);
        if (route is null)
        {
            return false;
        }

        db.Routes.Remove(route);
        db.AuditLogs.Add(new AuditLog
        {
            Id = IdGenerator.NewId("audit"),
            Type = "RouteDeleted",
            Actor = "system",
            TargetType = "Route",
            TargetId = route.Id,
            Message = $"删除路由：{route.Target}",
            CreatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static RouteDto ToDto(Route route) => new(
        route.Id,
        route.NetworkId,
        route.Type,
        route.Target,
        route.Via,
        route.Enabled,
        route.ProviderManaged);
}

public sealed record CreateRouteRequest(
    string Target,
    string? Via = null,
    string Type = "VirtualSubnet",
    bool Enabled = true,
    bool ProviderManaged = true);

public sealed record RouteDto(
    string Id,
    string NetworkId,
    string Type,
    string Target,
    string? Via,
    bool Enabled,
    bool ProviderManaged);
