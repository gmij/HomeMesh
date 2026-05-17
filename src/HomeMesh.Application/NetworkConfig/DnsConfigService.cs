using System.Text.Json;
using HomeMesh.Application.Setup;
using HomeMesh.Domain.Entities;
using HomeMesh.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeMesh.Application.NetworkConfig;

public sealed class DnsConfigService(HomeMeshDbContext db)
{
    public async Task<DnsConfigDto?> GetAsync(string networkId, CancellationToken cancellationToken = default)
    {
        var config = await db.DnsConfigs.FirstOrDefaultAsync(x => x.NetworkId == networkId, cancellationToken);
        return config is null ? null : ToDto(config);
    }

    public async Task<DnsConfigDto> UpsertAsync(string networkId, UpsertDnsConfigRequest request, CancellationToken cancellationToken = default)
    {
        var networkExists = await db.Networks.AnyAsync(x => x.Id == networkId, cancellationToken);
        if (!networkExists)
        {
            throw new InvalidOperationException("Network not found.");
        }

        var servers = request.Servers
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var now = DateTimeOffset.UtcNow;
        var config = await db.DnsConfigs.FirstOrDefaultAsync(x => x.NetworkId == networkId, cancellationToken);
        if (config is null)
        {
            config = new DnsConfig
            {
                Id = IdGenerator.NewId("dns"),
                NetworkId = networkId,
                CreatedAt = now
            };
            db.DnsConfigs.Add(config);
        }

        config.Domain = string.IsNullOrWhiteSpace(request.Domain) ? null : request.Domain.Trim();
        config.ServersJson = JsonSerializer.Serialize(servers);
        config.ProviderManaged = request.ProviderManaged;
        config.UpdatedAt = now;

        db.AuditLogs.Add(new AuditLog
        {
            Id = IdGenerator.NewId("audit"),
            Type = "DnsConfigUpdated",
            Actor = "system",
            TargetType = "DnsConfig",
            TargetId = config.Id,
            Message = "更新 DNS 配置",
            CreatedAt = now
        });

        await db.SaveChangesAsync(cancellationToken);
        return ToDto(config);
    }

    private static DnsConfigDto ToDto(DnsConfig config)
    {
        var servers = string.IsNullOrWhiteSpace(config.ServersJson)
            ? Array.Empty<string>()
            : JsonSerializer.Deserialize<string[]>(config.ServersJson) ?? Array.Empty<string>();

        return new DnsConfigDto(
            config.Id,
            config.NetworkId,
            config.Domain,
            servers,
            config.ProviderManaged);
    }
}

public sealed record UpsertDnsConfigRequest(
    string? Domain,
    IReadOnlyList<string> Servers,
    bool ProviderManaged = true);

public sealed record DnsConfigDto(
    string Id,
    string NetworkId,
    string? Domain,
    IReadOnlyList<string> Servers,
    bool ProviderManaged);
