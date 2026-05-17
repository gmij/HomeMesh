namespace HomeMesh.Domain.Entities;

public sealed class Device
{
    public string Id { get; set; } = default!;
    public string HomeId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Platform { get; set; } = "Unknown";
    public string? Fingerprint { get; set; }
    public string? PublicKey { get; set; }
    public string TrustLevel { get; set; } = "Normal";
    public string? TagsJson { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class NetworkMember
{
    public string Id { get; set; } = default!;
    public string NetworkId { get; set; } = default!;
    public string? DeviceId { get; set; }
    public string Provider { get; set; } = default!;
    public string ProviderMemberId { get; set; } = default!;
    public string? Name { get; set; }
    public string Role { get; set; } = "Unknown";
    public bool Authorized { get; set; }
    public bool ActiveBridge { get; set; }
    public bool Online { get; set; }
    public string? IpAssignmentsJson { get; set; }
    public string? TagsJson { get; set; }
    public string? ProviderRawJson { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
