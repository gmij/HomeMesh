using System.Text.Json;
using HomeMesh.Abstractions.Providers;
using HomeMesh.Application.Diagnostics;
using HomeMesh.Application.Setup;
using HomeMesh.Domain.Entities;
using HomeMesh.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HomeMesh.Application.Sync;

public sealed class NetworkConfigSyncService(
    HomeMeshDbContext db,
    IEnumerable<ISdwanControllerProvider> providers,
    ILogger<NetworkConfigSyncService> logger)
{
    public async Task<NetworkConfigSyncResultDto> SyncConfigAsync(string networkId, CancellationToken cancellationToken = default)
    {
        var network = await db.Networks.FirstOrDefaultAsync(x => x.Id == networkId, cancellationToken);
        if (network is null)
        {
            throw new InvalidOperationException("Network not found.");
        }

        var binding = await db.NetworkProviderBindings
            .Where(x => x.NetworkId == networkId)
            .OrderByDescending(x => x.IsPrimary)
            .FirstOrDefaultAsync(cancellationToken);

        if (binding is null)
        {
            throw new InvalidOperationException("Network provider binding not found.");
        }

        var provider = providers.FirstOrDefault(x => string.Equals(x.ProviderName, binding.Provider, StringComparison.OrdinalIgnoreCase));
        if (provider is null)
        {
            throw new InvalidOperationException($"Provider '{binding.Provider}' was not found.");
        }

        var routes = await db.Routes
            .Where(x => x.NetworkId == networkId && x.Enabled && x.ProviderManaged)
            .OrderBy(x => x.Target)
            .Select(x => new VirtualRoute(x.Target, x.Via))
            .ToListAsync(cancellationToken);

        var ipPools = await db.IpPools
            .Where(x => x.NetworkId == networkId && x.ProviderManaged)
            .OrderBy(x => x.IpRangeStart)
            .Select(x => new VirtualIpPool(x.IpRangeStart, x.IpRangeEnd))
            .ToListAsync(cancellationToken);

        var dns = await db.DnsConfigs.FirstOrDefaultAsync(x => x.NetworkId == networkId && x.ProviderManaged, cancellationToken);
        var dnsConfig = dns is null
            ? null
            : new VirtualDnsConfig(dns.Domain, DeserializeServers(dns.ServersJson));

        var now = DateTimeOffset.UtcNow;
        var state = await FindOrCreateSyncStateAsync(binding.Provider, "NetworkConfig", networkId, cancellationToken);

        try
        {
            var updated = await provider.UpdateNetworkAsync(
                binding.ProviderNetworkId,
                new UpdateVirtualNetworkRequest(
                    Name: network.Name,
                    Private: network.Private,
                    V4AssignMode: network.V4AssignMode,
                    Routes: routes,
                    IpPools: ipPools,
                    DnsConfig: dnsConfig),
                cancellationToken);

            state.Status = "Healthy";
            state.LastError = null;
            state.LastSyncAt = now;

            db.AuditLogs.Add(new AuditLog
            {
                Id = IdGenerator.NewId("audit"),
                HomeId = network.HomeId,
                Type = "NetworkConfigSynced",
                Actor = "system",
                TargetType = "Network",
                TargetId = network.Id,
                Message = $"同步网络配置：{network.Name}，路由 {routes.Count} 条，IP 池 {ipPools.Count} 个",
                CreatedAt = now
            });

            await db.SaveChangesAsync(cancellationToken);

            return new NetworkConfigSyncResultDto(
                network.Id,
                binding.Provider,
                binding.ProviderNetworkId,
                routes.Count,
                ipPools.Count,
                dnsConfig is not null,
                "Healthy",
                null,
                now,
                updated.ProviderNetworkId);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to sync config for network {NetworkId} via provider {Provider} ({ProviderNetworkId}).",
                network.Id,
                binding.Provider,
                binding.ProviderNetworkId);

            state.Status = "Error";
            state.LastError = ex.Message;
            state.LastSyncAt = now;

            db.AuditLogs.Add(new AuditLog
            {
                Id = IdGenerator.NewId("audit"),
                HomeId = network.HomeId,
                Type = "NetworkConfigSyncFailed",
                Actor = "system",
                TargetType = "Network",
                TargetId = network.Id,
                Message = $"Failed to sync network config for '{network.Name}'.",
                MetadataJson = ExceptionMetadataSerializer.Serialize(ex, new
                {
                    network.Id,
                    network.Name,
                    binding.Provider,
                    binding.ProviderNetworkId,
                    routeCount = routes.Count,
                    ipPoolCount = ipPools.Count,
                    hasDnsConfig = dnsConfig is not null,
                    operation = "SyncNetworkConfig"
                }),
                CreatedAt = now
            });

            await db.SaveChangesAsync(cancellationToken);

            return new NetworkConfigSyncResultDto(
                network.Id,
                binding.Provider,
                binding.ProviderNetworkId,
                routes.Count,
                ipPools.Count,
                dnsConfig is not null,
                "Error",
                ex.Message,
                now,
                null);
        }
    }

    private async Task<ProviderSyncState> FindOrCreateSyncStateAsync(
        string provider,
        string resourceType,
        string resourceId,
        CancellationToken cancellationToken)
    {
        var state = await db.ProviderSyncStates.FirstOrDefaultAsync(
            x => x.Provider == provider && x.ResourceType == resourceType && x.ResourceId == resourceId,
            cancellationToken);

        if (state is not null)
        {
            return state;
        }

        state = new ProviderSyncState
        {
            Id = IdGenerator.NewId("sync"),
            Provider = provider,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Status = "Pending"
        };

        db.ProviderSyncStates.Add(state);
        return state;
    }

    private static IReadOnlyList<string> DeserializeServers(string? serversJson)
    {
        if (string.IsNullOrWhiteSpace(serversJson))
        {
            return Array.Empty<string>();
        }

        return JsonSerializer.Deserialize<string[]>(serversJson) ?? Array.Empty<string>();
    }
}

public sealed record NetworkConfigSyncResultDto(
    string NetworkId,
    string Provider,
    string ProviderNetworkId,
    int RouteCount,
    int IpPoolCount,
    bool HasDnsConfig,
    string Status,
    string? Error,
    DateTimeOffset SyncedAt,
    string? UpdatedProviderNetworkId);
