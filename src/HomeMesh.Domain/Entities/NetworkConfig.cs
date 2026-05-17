namespace HomeMesh.Domain.Entities;

public sealed class Route
{
    public string Id { get; set; } = default!;
    public string NetworkId { get; set; } = default!;
    public string Type { get; set; } = "VirtualSubnet";
    public string Target { get; set; } = default!;
    public string? Via { get; set; }
    public bool Enabled { get; set; } = true;
    public bool ProviderManaged { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class IpPool
{
    public string Id { get; set; } = default!;
    public string NetworkId { get; set; } = default!;
    public string IpRangeStart { get; set; } = default!;
    public string IpRangeEnd { get; set; } = default!;
    public bool ProviderManaged { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class DnsConfig
{
    public string Id { get; set; } = default!;
    public string NetworkId { get; set; } = default!;
    public string? Domain { get; set; }
    public string ServersJson { get; set; } = "[]";
    public string? SplitDnsJson { get; set; }
    public bool ProviderManaged { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Gateway
{
    public string Id { get; set; } = default!;
    public string NetworkId { get; set; } = default!;
    public string? DeviceId { get; set; }
    public string MemberId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? LanSubnetsJson { get; set; }
    public bool SubnetRouterEnabled { get; set; }
    public bool ExitNodeEnabled { get; set; }
    public string HealthStatus { get; set; } = "Unknown";
    public DateTimeOffset? LastHeartbeatAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
