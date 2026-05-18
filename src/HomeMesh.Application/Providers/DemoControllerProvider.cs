using System.Collections.Concurrent;
using HomeMesh.Abstractions.Providers;

namespace HomeMesh.Application.Providers;

public sealed class DemoControllerProvider : ISdwanControllerProvider
{
    private static readonly ConcurrentDictionary<string, DemoNetworkState> Networks = new();

    public string ProviderName => "Demo";

    public Task<ProviderHealthStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ProviderHealthStatus(
            ProviderName,
            "Healthy",
            "Demo provider is running in memory. It is intended for MVP demos without ZeroTier One.",
            DateTimeOffset.UtcNow));
    }

    public Task<IReadOnlyList<VirtualNetworkInfo>> ListNetworksAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<VirtualNetworkInfo>>(Networks.Values.Select(x => x.Network).ToArray());
    }

    public Task<VirtualNetworkInfo> CreateNetworkAsync(CreateVirtualNetworkRequest request, CancellationToken cancellationToken = default)
    {
        var providerNetworkId = $"demo_{Guid.NewGuid():N}";
        var routes = string.IsNullOrWhiteSpace(request.Cidr)
            ? Array.Empty<VirtualRoute>()
            : new[] { new VirtualRoute(request.Cidr, null) };

        var network = new VirtualNetworkInfo(
            providerNetworkId,
            request.Name,
            request.Cidr,
            request.Private,
            routes,
            Array.Empty<VirtualIpPool>(),
            null);

        var members = new ConcurrentDictionary<string, VirtualMemberInfo>();
        members.TryAdd("demo-laptop", new VirtualMemberInfo("demo-laptop", "演示笔记本", false, false, true, Array.Empty<string>()));
        members.TryAdd("demo-phone", new VirtualMemberInfo("demo-phone", "演示手机", false, false, false, Array.Empty<string>()));

        Networks[providerNetworkId] = new DemoNetworkState(network, members);
        return Task.FromResult(network);
    }

    public Task<VirtualNetworkInfo> GetNetworkAsync(string providerNetworkId, CancellationToken cancellationToken = default)
    {
        if (!Networks.TryGetValue(providerNetworkId, out var state))
        {
            throw new InvalidOperationException($"Demo network '{providerNetworkId}' was not found.");
        }

        return Task.FromResult(state.Network);
    }

    public Task<VirtualNetworkInfo> UpdateNetworkAsync(string providerNetworkId, UpdateVirtualNetworkRequest request, CancellationToken cancellationToken = default)
    {
        if (!Networks.TryGetValue(providerNetworkId, out var state))
        {
            throw new InvalidOperationException($"Demo network '{providerNetworkId}' was not found.");
        }

        var network = state.Network;
        var updated = network with
        {
            Name = request.Name ?? network.Name,
            Private = request.Private ?? network.Private,
            Routes = request.Routes ?? network.Routes,
            IpPools = request.IpPools ?? network.IpPools,
            DnsConfig = request.DnsConfig ?? network.DnsConfig
        };

        Networks[providerNetworkId] = state with { Network = updated };
        return Task.FromResult(updated);
    }

    public Task DeleteNetworkAsync(string providerNetworkId, CancellationToken cancellationToken = default)
    {
        Networks.TryRemove(providerNetworkId, out _);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<VirtualMemberInfo>> ListMembersAsync(string providerNetworkId, CancellationToken cancellationToken = default)
    {
        if (!Networks.TryGetValue(providerNetworkId, out var state))
        {
            throw new InvalidOperationException($"Demo network '{providerNetworkId}' was not found.");
        }

        return Task.FromResult<IReadOnlyList<VirtualMemberInfo>>(state.Members.Values.ToArray());
    }

    public Task<VirtualMemberInfo> UpdateMemberAsync(string providerNetworkId, string providerMemberId, UpdateVirtualMemberRequest request, CancellationToken cancellationToken = default)
    {
        if (!Networks.TryGetValue(providerNetworkId, out var state))
        {
            throw new InvalidOperationException($"Demo network '{providerNetworkId}' was not found.");
        }

        if (!state.Members.TryGetValue(providerMemberId, out var member))
        {
            member = new VirtualMemberInfo(providerMemberId, providerMemberId, false, false, false, Array.Empty<string>());
        }

        var updated = member with
        {
            Name = request.Name ?? member.Name,
            Authorized = request.Authorized ?? member.Authorized,
            ActiveBridge = request.ActiveBridge ?? member.ActiveBridge,
            IpAssignments = request.IpAssignments ?? member.IpAssignments
        };

        state.Members[providerMemberId] = updated;
        return Task.FromResult(updated);
    }

    private sealed record DemoNetworkState(
        VirtualNetworkInfo Network,
        ConcurrentDictionary<string, VirtualMemberInfo> Members);
}
