namespace HomeMesh.Domain.Entities;

public sealed class User
{
    public string Id { get; set; } = default!;
    public string Username { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string Role { get; set; } = "Admin";
    public bool Disabled { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLoginAt { get; set; }
}

public sealed class EnrollmentToken
{
    public string Id { get; set; } = default!;
    public string HomeId { get; set; } = default!;
    public string NetworkId { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string? DefaultTagsJson { get; set; }
    public bool AutoAuthorize { get; set; }
    public int? MaxUses { get; set; }
    public int UsedCount { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AclPolicy
{
    public string Id { get; set; } = default!;
    public string HomeId { get; set; } = default!;
    public string? NetworkId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool Enabled { get; set; } = true;
    public int Priority { get; set; } = 1000;
    public string RulesJson { get; set; } = "[]";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AuditLog
{
    public string Id { get; set; } = default!;
    public string? HomeId { get; set; }
    public string? UserId { get; set; }
    public string Type { get; set; } = default!;
    public string Actor { get; set; } = "system";
    public string? TargetType { get; set; }
    public string? TargetId { get; set; }
    public string Message { get; set; } = default!;
    public string? MetadataJson { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class ProviderSyncState
{
    public string Id { get; set; } = default!;
    public string Provider { get; set; } = default!;
    public string ResourceType { get; set; } = default!;
    public string? ResourceId { get; set; }
    public DateTimeOffset? LastSyncAt { get; set; }
    public string? LastError { get; set; }
    public string Status { get; set; } = "Unknown";
}
