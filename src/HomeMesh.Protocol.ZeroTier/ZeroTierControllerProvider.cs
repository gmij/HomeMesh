using HomeMesh.Abstractions.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HomeMesh.Protocol.ZeroTier;

public sealed class ZeroTierControllerProvider(
    IOptions<ZeroTierOptions> options,
    ZeroTierLocalApiClient client,
    ILogger<ZeroTierControllerProvider> logger) : ISdwanControllerProvider
{
    private readonly ZeroTierOptions _options = options.Value;

    public string ProviderName => "ZeroTier";

    public async Task<ProviderHealthStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return new ProviderHealthStatus(ProviderName, "Disabled", "ZeroTier provider is disabled.", DateTimeOffset.UtcNow);
        }

        try
        {
            var status = await client.GetStatusAsync(cancellationToken);
            var message = status is null
                ? "ZeroTier local API returned an empty status."
                : $"ZeroTier node {status.Address} is {(status.Online ? "online" : "offline")}. Version: {status.Version ?? "unknown"}.";

            return new ProviderHealthStatus(ProviderName, status?.Online == true ? "Healthy" : "Warning", message, DateTimeOffset.UtcNow);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read ZeroTier provider health status.");
            return new ProviderHealthStatus(ProviderName, "Error", ex.Message, DateTimeOffset.UtcNow);
        }
    }

    public async Task<IReadOnlyList<VirtualNetworkInfo>> ListNetworksAsync(CancellationToken cancellationToken = default)
    {
        var networkIds = await client.ListNetworkIdsAsync(cancellationToken);
        var networks = new List<VirtualNetworkInfo>();

        foreach (var networkId in networkIds)
        {
            var network = await client.GetNetworkAsync(networkId, cancellationToken);
            if (network is not null)
            {
                networks.Add(MapNetwork(network));
            }
        }

        return networks;
    }

    public async Task<VirtualNetworkInfo> CreateNetworkAsync(CreateVirtualNetworkRequest request, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            throw new InvalidOperationException("ZeroTier provider is disabled.");
        }

        var network = await client.CreateNetworkAsync(request.Name, cancellationToken)
            ?? throw new InvalidOperationException("ZeroTier local API returned an empty network after create.");

        if (request.Cidr is not null || request.Private != network.Private)
        {
            var routes = new List<ZeroTierRoute>(network.Routes);
            if (request.Cidr is not null)
            {
                routes.Add(new ZeroTierRoute { Target = request.Cidr });
            }

            network = await client.UpdateNetworkAsync(network.Nwid!, new
            {
                name = request.Name,
                @private = request.Private,
                routes
            }, cancellationToken) ?? network;
        }

        return MapNetwork(network);
    }

    public async Task<VirtualNetworkInfo> GetNetworkAsync(string providerNetworkId, CancellationToken cancellationToken = default)
    {
        var network = await client.GetNetworkAsync(providerNetworkId, cancellationToken)
            ?? throw new InvalidOperationException($"ZeroTier network '{providerNetworkId}' was not found.");

        return MapNetwork(network);
    }

    public async Task<VirtualNetworkInfo> UpdateNetworkAsync(string providerNetworkId, UpdateVirtualNetworkRequest request, CancellationToken cancellationToken = default)
    {
        var patch = new Dictionary<string, object?>();
        if (request.Name is not null) patch["name"] = request.Name;
        if (request.Private.HasValue) patch["private"] = request.Private.Value;
        if (request.Routes is not null) patch["routes"] = request.Routes.Select(x => new { x.Target, x.Via }).ToArray();
        if (request.IpPools is not null) patch["ipAssignmentPools"] = request.IpPools.Select(x => new { x.IpRangeStart, x.IpRangeEnd }).ToArray();
        if (request.DnsConfig is not null) patch["dns"] = new { request.DnsConfig.Domain, request.DnsConfig.Servers };

        var network = await client.UpdateNetworkAsync(providerNetworkId, patch, cancellationToken)
            ?? throw new InvalidOperationException($"ZeroTier network '{providerNetworkId}' update returned empty response.");

        return MapNetwork(network);
    }

    public Task DeleteNetworkAsync(string providerNetworkId, CancellationToken cancellationToken = default)
    {
        return client.DeleteNetworkAsync(providerNetworkId, cancellationToken);
    }

    public async Task<IReadOnlyList<VirtualMemberInfo>> ListMembersAsync(string providerNetworkId, CancellationToken cancellationToken = default)
    {
        var memberIds = await client.ListMemberIdsAsync(providerNetworkId, cancellationToken);
        var peers = await client.ListPeersAsync(cancellationToken);
        var members = new List<VirtualMemberInfo>();

        foreach (var memberId in memberIds)
        {
            var member = await client.GetMemberAsync(providerNetworkId, memberId, cancellationToken);
            if (member is not null)
            {
                var online = peers.Any(x => string.Equals(x.Address, member.Address ?? member.Id, StringComparison.OrdinalIgnoreCase));
                members.Add(MapMember(member, online));
            }
        }

        return members;
    }

    public async Task<VirtualMemberInfo> UpdateMemberAsync(string providerNetworkId, string providerMemberId, UpdateVirtualMemberRequest request, CancellationToken cancellationToken = default)
    {
        var patch = new Dictionary<string, object?>();
        if (request.Authorized.HasValue) patch["authorized"] = request.Authorized.Value;
        if (request.ActiveBridge.HasValue) patch["activeBridge"] = request.ActiveBridge.Value;
        if (request.IpAssignments is not null) patch["ipAssignments"] = request.IpAssignments;

        var member = await client.UpdateMemberAsync(providerNetworkId, providerMemberId, patch, cancellationToken)
            ?? throw new InvalidOperationException($"ZeroTier member '{providerMemberId}' update returned empty response.");

        return MapMember(member, online: false);
    }

    private static VirtualNetworkInfo MapNetwork(ZeroTierNetwork network)
    {
        return new VirtualNetworkInfo(
            network.Nwid ?? string.Empty,
            network.Name ?? string.Empty,
            network.Routes.FirstOrDefault()?.Target,
            network.Private,
            network.Routes
                .Where(x => !string.IsNullOrWhiteSpace(x.Target))
                .Select(x => new VirtualRoute(x.Target!, x.Via))
                .ToArray(),
            network.IpAssignmentPools
                .Where(x => !string.IsNullOrWhiteSpace(x.IpRangeStart) && !string.IsNullOrWhiteSpace(x.IpRangeEnd))
                .Select(x => new VirtualIpPool(x.IpRangeStart!, x.IpRangeEnd!))
                .ToArray(),
            network.Dns is null ? null : new VirtualDnsConfig(network.Dns.Domain, network.Dns.Servers));
    }

    private static VirtualMemberInfo MapMember(ZeroTierMember member, bool online)
    {
        return new VirtualMemberInfo(
            member.Address ?? member.Id ?? string.Empty,
            null,
            member.Authorized,
            member.ActiveBridge,
            online,
            member.IpAssignments);
    }
}
