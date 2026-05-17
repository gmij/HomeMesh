using HomeMesh.Abstractions.Providers;
using Microsoft.Extensions.Options;

namespace HomeMesh.Protocol.ZeroTier;

public sealed class ZeroTierControllerProvider(IOptions<ZeroTierOptions> options) : ISdwanControllerProvider
{
    private readonly ZeroTierOptions _options = options.Value;

    public string ProviderName => "ZeroTier";

    public Task<ProviderHealthStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var status = _options.Enabled ? "Configured" : "Disabled";
        var message = _options.Enabled
            ? $"ZeroTier provider configured at {_options.ApiBaseUrl}. Real API integration is pending."
            : "ZeroTier provider is disabled.";

        return Task.FromResult(new ProviderHealthStatus(ProviderName, status, message, DateTimeOffset.UtcNow));
    }

    public Task<IReadOnlyList<VirtualNetworkInfo>> ListNetworksAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<VirtualNetworkInfo>>([]);
    }

    public Task<VirtualNetworkInfo> CreateNetworkAsync(CreateVirtualNetworkRequest request, CancellationToken cancellationToken = default)
    {
        var network = new VirtualNetworkInfo(
            ProviderNetworkId: $"zt_stub_{Guid.NewGuid():N}"[..24],
            Name: request.Name,
            Cidr: request.Cidr,
            Private: request.Private,
            Routes: [],
            IpPools: [],
            DnsConfig: null);

        return Task.FromResult(network);
    }

    public Task<VirtualNetworkInfo> GetNetworkAsync(string providerNetworkId, CancellationToken cancellationToken = default)
    {
        var network = new VirtualNetworkInfo(providerNetworkId, "Stub Network", null, true, [], [], null);
        return Task.FromResult(network);
    }

    public Task<VirtualNetworkInfo> UpdateNetworkAsync(string providerNetworkId, UpdateVirtualNetworkRequest request, CancellationToken cancellationToken = default)
    {
        var network = new VirtualNetworkInfo(
            providerNetworkId,
            request.Name ?? "Stub Network",
            null,
            request.Private ?? true,
            request.Routes ?? [],
            request.IpPools ?? [],
            request.DnsConfig);

        return Task.FromResult(network);
    }

    public Task DeleteNetworkAsync(string providerNetworkId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<VirtualMemberInfo>> ListMembersAsync(string providerNetworkId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<VirtualMemberInfo>>([]);
    }

    public Task<VirtualMemberInfo> UpdateMemberAsync(string providerNetworkId, string providerMemberId, UpdateVirtualMemberRequest request, CancellationToken cancellationToken = default)
    {
        var member = new VirtualMemberInfo(
            providerMemberId,
            request.Name,
            request.Authorized ?? false,
            request.ActiveBridge ?? false,
            Online: false,
            IpAssignments: request.IpAssignments ?? []);

        return Task.FromResult(member);
    }
}
