namespace HomeMesh.Domain.Entities;

public sealed class Home
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? OwnerUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Network
{
    public string Id { get; set; } = default!;
    public string HomeId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Cidr { get; set; }
    public bool Private { get; set; } = true;
    public bool V4AssignMode { get; set; } = true;
    public bool AutoApproveMembers { get; set; }
    public string? V6AssignModeJson { get; set; }
    public string Status { get; set; } = "Unknown";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class NetworkProviderBinding
{
    public string Id { get; set; } = default!;
    public string NetworkId { get; set; } = default!;
    public string Provider { get; set; } = default!;
    public string ProviderNetworkId { get; set; } = default!;
    public string? ProviderConfigJson { get; set; }
    public bool IsPrimary { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
