namespace HomeMesh.Application.NetworkConfig;

public sealed record EasySetupRequest(
    string Cidr,
    string IpPoolStart,
    string IpPoolEnd,
    bool EnableAutoAssign = true,
    string? DnsDomain = null,
    IReadOnlyList<string>? DnsServers = null);

public sealed record EasySetupResultDto(
    string NetworkId,
    string Cidr,
    string IpPoolStart,
    string IpPoolEnd,
    bool EnableAutoAssign);
