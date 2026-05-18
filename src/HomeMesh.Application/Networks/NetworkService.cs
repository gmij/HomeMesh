using HomeMesh.Abstractions.Providers;
using HomeMesh.Application.Setup;
using HomeMesh.Domain.Entities;
using HomeMesh.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeMesh.Application.Networks;

public sealed class NetworkService(HomeMeshDbContext db, IEnumerable<ISdwanControllerProvider> providers)
{
    public async Task<IReadOnlyList<NetworkSummaryDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await db.Networks
            .OrderBy(x => x.Name)
            .Select(x => new NetworkSummaryDto(
                x.Id,
                x.HomeId,
                x.Name,
                x.Cidr,
                x.Private,
                x.V4AssignMode,
                x.AutoApproveMembers,
                x.Status,
                x.CreatedAt,
                x.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<NetworkDetailDto?> GetAsync(string networkId, CancellationToken cancellationToken = default)
    {
        var network = await db.Networks.FirstOrDefaultAsync(x => x.Id == networkId, cancellationToken);
        if (network is null)
        {
            return null;
        }

        var bindings = await db.NetworkProviderBindings
            .Where(x => x.NetworkId == networkId)
            .OrderByDescending(x => x.IsPrimary)
            .Select(x => new NetworkProviderBindingDto(
                x.Provider,
                x.ProviderNetworkId,
                x.IsPrimary,
                x.CreatedAt,
                x.UpdatedAt))
            .ToListAsync(cancellationToken);

        var memberCount = await db.NetworkMembers.CountAsync(x => x.NetworkId == networkId, cancellationToken);
        var onlineMemberCount = await db.NetworkMembers.CountAsync(x => x.NetworkId == networkId && x.Online, cancellationToken);
        var routeCount = await db.Routes.CountAsync(x => x.NetworkId == networkId, cancellationToken);
        var gatewayCount = await db.Gateways.CountAsync(x => x.NetworkId == networkId, cancellationToken);

        return new NetworkDetailDto(
            network.Id,
            network.HomeId,
            network.Name,
            network.Cidr,
            network.Private,
            network.V4AssignMode,
            network.AutoApproveMembers,
            network.Status,
            memberCount,
            onlineMemberCount,
            routeCount,
            gatewayCount,
            bindings,
            network.CreatedAt,
            network.UpdatedAt);
    }

    public async Task<NetworkSummaryDto> CreateAsync(CreateNetworkRequest request, CancellationToken cancellationToken = default)
    {
        var home = await db.Homes.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        if (home is null)
        {
            throw new InvalidOperationException("HomeMesh is not initialized. Please create the first administrator first.");
        }

        var provider = providers.FirstOrDefault(x => string.Equals(x.ProviderName, request.Provider, StringComparison.OrdinalIgnoreCase));
        if (provider is null)
        {
            throw new InvalidOperationException($"Provider '{request.Provider}' was not found.");
        }

        var virtualNetwork = await provider.CreateNetworkAsync(
            new CreateVirtualNetworkRequest(request.Name, request.Cidr, request.Private),
            cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var network = new Network
        {
            Id = IdGenerator.NewId("hmnet"),
            HomeId = home.Id,
            Name = request.Name.Trim(),
            Cidr = request.Cidr,
            Private = request.Private,
            V4AssignMode = true,
            AutoApproveMembers = request.AutoApproveMembers,
            Status = "Created",
            CreatedAt = now,
            UpdatedAt = now
        };

        var binding = new NetworkProviderBinding
        {
            Id = IdGenerator.NewId("npb"),
            NetworkId = network.Id,
            Provider = provider.ProviderName,
            ProviderNetworkId = virtualNetwork.ProviderNetworkId,
            IsPrimary = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Networks.Add(network);
        db.NetworkProviderBindings.Add(binding);
        db.AuditLogs.Add(new AuditLog
        {
            Id = IdGenerator.NewId("audit"),
            HomeId = home.Id,
            Type = "NetworkCreated",
            Actor = "system",
            TargetType = "Network",
            TargetId = network.Id,
            Message = $"创建网络：{network.Name}",
            CreatedAt = now
        });

        await db.SaveChangesAsync(cancellationToken);

        return ToSummary(network);
    }

    public async Task<bool> DeleteAsync(string networkId, CancellationToken cancellationToken = default)
    {
        var network = await db.Networks.FirstOrDefaultAsync(x => x.Id == networkId, cancellationToken);
        if (network is null)
        {
            return false;
        }

        var bindings = await db.NetworkProviderBindings.Where(x => x.NetworkId == networkId).ToListAsync(cancellationToken);
        foreach (var binding in bindings)
        {
            var provider = providers.FirstOrDefault(x => string.Equals(x.ProviderName, binding.Provider, StringComparison.OrdinalIgnoreCase));
            if (provider is not null)
            {
                await provider.DeleteNetworkAsync(binding.ProviderNetworkId, cancellationToken);
            }
        }

        db.Routes.RemoveRange(db.Routes.Where(x => x.NetworkId == networkId));
        db.IpPools.RemoveRange(db.IpPools.Where(x => x.NetworkId == networkId));
        db.DnsConfigs.RemoveRange(db.DnsConfigs.Where(x => x.NetworkId == networkId));
        db.Gateways.RemoveRange(db.Gateways.Where(x => x.NetworkId == networkId));
        db.NetworkMembers.RemoveRange(db.NetworkMembers.Where(x => x.NetworkId == networkId));
        db.NetworkProviderBindings.RemoveRange(bindings);
        db.Networks.Remove(network);

        db.AuditLogs.Add(new AuditLog
        {
            Id = IdGenerator.NewId("audit"),
            HomeId = network.HomeId,
            Type = "NetworkDeleted",
            Actor = "system",
            TargetType = "Network",
            TargetId = network.Id,
            Message = $"删除网络：{network.Name}",
            CreatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static NetworkSummaryDto ToSummary(Network network) => new(
        network.Id,
        network.HomeId,
        network.Name,
        network.Cidr,
        network.Private,
        network.V4AssignMode,
        network.AutoApproveMembers,
        network.Status,
        network.CreatedAt,
        network.UpdatedAt);
}

public sealed record CreateNetworkRequest(
    string Name,
    string Provider = "ZeroTier",
    string? Cidr = null,
    bool Private = true,
    bool AutoApproveMembers = false);

public sealed record NetworkSummaryDto(
    string Id,
    string HomeId,
    string Name,
    string? Cidr,
    bool Private,
    bool V4AssignMode,
    bool AutoApproveMembers,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record NetworkDetailDto(
    string Id,
    string HomeId,
    string Name,
    string? Cidr,
    bool Private,
    bool V4AssignMode,
    bool AutoApproveMembers,
    string Status,
    int MemberCount,
    int OnlineMemberCount,
    int RouteCount,
    int GatewayCount,
    IReadOnlyList<NetworkProviderBindingDto> ProviderBindings,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record NetworkProviderBindingDto(
    string Provider,
    string ProviderNetworkId,
    bool IsPrimary,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
