namespace HomeMesh.Abstractions.Providers;

public interface ISdwanControllerProvider
{
    string ProviderName { get; }

    Task<ProviderHealthStatus> GetStatusAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VirtualNetworkInfo>> ListNetworksAsync(CancellationToken cancellationToken = default);

    Task<VirtualNetworkInfo> CreateNetworkAsync(CreateVirtualNetworkRequest request, CancellationToken cancellationToken = default);

    Task<VirtualNetworkInfo> GetNetworkAsync(string providerNetworkId, CancellationToken cancellationToken = default);

    Task<VirtualNetworkInfo> UpdateNetworkAsync(string providerNetworkId, UpdateVirtualNetworkRequest request, CancellationToken cancellationToken = default);

    Task DeleteNetworkAsync(string providerNetworkId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VirtualMemberInfo>> ListMembersAsync(string providerNetworkId, CancellationToken cancellationToken = default);

    Task<VirtualMemberInfo> UpdateMemberAsync(string providerNetworkId, string providerMemberId, UpdateVirtualMemberRequest request, CancellationToken cancellationToken = default);
}

public sealed record ProviderHealthStatus(
    string ProviderName,
    string Status,
    string? Message = null,
    DateTimeOffset CheckedAt = default);

public sealed record VirtualNetworkInfo(
    string ProviderNetworkId,
    string Name,
    string? Cidr,
    bool Private,
    IReadOnlyList<VirtualRoute> Routes,
    IReadOnlyList<VirtualIpPool> IpPools,
    VirtualDnsConfig? DnsConfig);

public sealed record VirtualMemberInfo(
    string ProviderMemberId,
    string? Name,
    bool Authorized,
    bool ActiveBridge,
    bool Online,
    IReadOnlyList<string> IpAssignments,
    string? RawJson = null);

public sealed record VirtualRoute(string Target, string? Via);

public sealed record VirtualIpPool(string IpRangeStart, string IpRangeEnd);

public sealed record VirtualDnsConfig(string? Domain, IReadOnlyList<string> Servers);

public sealed record CreateVirtualNetworkRequest(string Name, string? Cidr = null, bool Private = true);

public sealed record UpdateVirtualNetworkRequest(
    string? Name = null,
    bool? Private = null,
    bool? V4AssignMode = null,
    object? V6AssignMode = null,
    IReadOnlyList<VirtualRoute>? Routes = null,
    IReadOnlyList<VirtualIpPool>? IpPools = null,
    VirtualDnsConfig? DnsConfig = null);

public sealed record UpdateVirtualMemberRequest(
    string? Name = null,
    bool? Authorized = null,
    bool? ActiveBridge = null,
    IReadOnlyList<string>? IpAssignments = null);
