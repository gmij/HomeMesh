using System.Text.Json;
using HomeMesh.Application.Setup;
using HomeMesh.Domain.Entities;
using HomeMesh.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeMesh.Application.NetworkConfig;

public sealed class EasySetupService(HomeMeshDbContext db)
{
    public async Task<EasySetupResultDto> ApplyAsync(string networkId, EasySetupRequest request, CancellationToken cancellationToken = default)
    {
        var network = await db.Networks.FirstOrDefaultAsync(x => x.Id == networkId, cancellationToken);
        if (network is null)
        {
            throw new InvalidOperationException("Network not found.");
        }

        var cidr = NormalizeRequired(request.Cidr, "CIDR is required.");
        if (!cidr.Contains('/'))
        {
            throw new InvalidOperationException("CIDR must include prefix length.");
        }

        var poolStart = NormalizeRequired(request.IpPoolStart, "IP pool start is required.");
        var poolEnd = NormalizeRequired(request.IpPoolEnd, "IP pool end is required.");
        var now = DateTimeOffset.UtcNow;

        network.Cidr = cidr;
        network.V4AssignMode = request.EnableAutoAssign;
        network.UpdatedAt = now;

        await UpsertRouteAsync(networkId, cidr, now, cancellationToken);
        await UpsertIpPoolAsync(networkId, poolStart, poolEnd, now, cancellationToken);
        await UpsertDnsAsync(networkId, request, now, cancellationToken);

        db.AuditLogs.Add(new AuditLog
        {
            Id = IdGenerator.NewId("audit"),
            HomeId = network.HomeId,
            Type = "EasySetupApplied",
            Actor = "system",
            TargetType = "Network",
            TargetId = network.Id,
            Message = $"Applied easy setup for network {network.Name} with CIDR {cidr}.",
            CreatedAt = now
        });

        await db.SaveChangesAsync(cancellationToken);
        return new EasySetupResultDto(network.Id, cidr, poolStart, poolEnd, request.EnableAutoAssign);
    }

    private async Task UpsertRouteAsync(string networkId, string cidr, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var route = await db.Routes.FirstOrDefaultAsync(x => x.NetworkId == networkId && x.Target == cidr, cancellationToken);
        if (route is null)
        {
            db.Routes.Add(new Route
            {
                Id = IdGenerator.NewId("route"),
                NetworkId = networkId,
                Type = "VirtualSubnet",
                Target = cidr,
                Enabled = true,
                ProviderManaged = true,
                CreatedAt = now,
                UpdatedAt = now
            });
            return;
        }

        route.Enabled = true;
        route.ProviderManaged = true;
        route.UpdatedAt = now;
    }

    private async Task UpsertIpPoolAsync(string networkId, string poolStart, string poolEnd, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var pool = await db.IpPools.FirstOrDefaultAsync(
            x => x.NetworkId == networkId && x.IpRangeStart == poolStart && x.IpRangeEnd == poolEnd,
            cancellationToken);

        if (pool is null)
        {
            db.IpPools.Add(new IpPool
            {
                Id = IdGenerator.NewId("ippool"),
                NetworkId = networkId,
                IpRangeStart = poolStart,
                IpRangeEnd = poolEnd,
                ProviderManaged = true,
                CreatedAt = now,
                UpdatedAt = now
            });
            return;
        }

        pool.ProviderManaged = true;
        pool.UpdatedAt = now;
    }

    private async Task UpsertDnsAsync(string networkId, EasySetupRequest request, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (request.DnsServers is null && request.DnsDomain is null)
        {
            return;
        }

        var dns = await db.DnsConfigs.FirstOrDefaultAsync(x => x.NetworkId == networkId, cancellationToken);
        if (dns is null)
        {
            dns = new DnsConfig
            {
                Id = IdGenerator.NewId("dns"),
                NetworkId = networkId,
                CreatedAt = now
            };
            db.DnsConfigs.Add(dns);
        }

        dns.Domain = string.IsNullOrWhiteSpace(request.DnsDomain) ? dns.Domain : request.DnsDomain.Trim();
        dns.ServersJson = JsonSerializer.Serialize(request.DnsServers ?? Array.Empty<string>());
        dns.ProviderManaged = true;
        dns.UpdatedAt = now;
    }

    private static string NormalizeRequired(string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(message);
        }

        return value.Trim();
    }
}
