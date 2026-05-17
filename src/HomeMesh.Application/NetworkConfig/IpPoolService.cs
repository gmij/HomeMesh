using HomeMesh.Domain.Entities;
using HomeMesh.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeMesh.Application.NetworkConfig;

public sealed class IpPoolService(HomeMeshDbContext db)
{
    public async Task<IReadOnlyList<IpPoolDto>> ListAsync(string networkId, CancellationToken cancellationToken = default)
    {
        return await db.IpPools
            .Where(x => x.NetworkId == networkId)
            .OrderBy(x => x.IpRangeStart)
            .Select(x => new IpPoolDto(x.Id, x.NetworkId, x.IpRangeStart, x.IpRangeEnd, x.ProviderManaged))
            .ToListAsync(cancellationToken);
    }

    public async Task<IpPoolDto> CreateAsync(string networkId, CreateIpPoolRequest request, CancellationToken cancellationToken = default)
    {
        var networkExists = await db.Networks.AnyAsync(x => x.Id == networkId, cancellationToken);
        if (!networkExists)
        {
            throw new InvalidOperationException("Network not found.");
        }

        if (string.IsNullOrWhiteSpace(request.IpRangeStart) || string.IsNullOrWhiteSpace(request.IpRangeEnd))
        {
            throw new InvalidOperationException("IP range start and end are required.");
        }

        var now = DateTimeOffset.UtcNow;
        var pool = new IpPool
        {
            Id = IdGenerator.NewId("ippool"),
            NetworkId = networkId,
            IpRangeStart = request.IpRangeStart.Trim(),
            IpRangeEnd = request.IpRangeEnd.Trim(),
            ProviderManaged = request.ProviderManaged,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.IpPools.Add(pool);
        db.AuditLogs.Add(new AuditLog
        {
            Id = IdGenerator.NewId("audit"),
            Type = "IpPoolCreated",
            Actor = "system",
            TargetType = "IpPool",
            TargetId = pool.Id,
            Message = $"新增 IP 池：{pool.IpRangeStart} - {pool.IpRangeEnd}",
            CreatedAt = now
        });

        await db.SaveChangesAsync(cancellationToken);
        return ToDto(pool);
    }

    public async Task<bool> DeleteAsync(string networkId, string poolId, CancellationToken cancellationToken = default)
    {
        var pool = await db.IpPools.FirstOrDefaultAsync(x => x.NetworkId == networkId && x.Id == poolId, cancellationToken);
        if (pool is null)
        {
            return false;
        }

        db.IpPools.Remove(pool);
        db.AuditLogs.Add(new AuditLog
        {
            Id = IdGenerator.NewId("audit"),
            Type = "IpPoolDeleted",
            Actor = "system",
            TargetType = "IpPool",
            TargetId = pool.Id,
            Message = $"删除 IP 池：{pool.IpRangeStart} - {pool.IpRangeEnd}",
            CreatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static IpPoolDto ToDto(IpPool pool) => new(
        pool.Id,
        pool.NetworkId,
        pool.IpRangeStart,
        pool.IpRangeEnd,
        pool.ProviderManaged);
}

public sealed record CreateIpPoolRequest(
    string IpRangeStart,
    string IpRangeEnd,
    bool ProviderManaged = true);

public sealed record IpPoolDto(
    string Id,
    string NetworkId,
    string IpRangeStart,
    string IpRangeEnd,
    bool ProviderManaged);
