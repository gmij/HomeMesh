using System.Text.Json;
using HomeMesh.Abstractions.Providers;
using HomeMesh.Application.Setup;
using HomeMesh.Domain.Entities;
using HomeMesh.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeMesh.Application.Members;

public sealed class MemberService(HomeMeshDbContext db, IEnumerable<ISdwanControllerProvider> providers)
{
    public async Task<IReadOnlyList<MemberDto>> ListAsync(string networkId, CancellationToken cancellationToken = default)
    {
        return await db.NetworkMembers
            .Where(x => x.NetworkId == networkId)
            .OrderBy(x => x.Name ?? x.ProviderMemberId)
            .Select(x => new MemberDto(
                x.Id,
                x.NetworkId,
                x.DeviceId,
                x.Provider,
                x.ProviderMemberId,
                x.Name,
                x.Role,
                x.Authorized,
                x.ActiveBridge,
                x.Online,
                x.IpAssignmentsJson,
                x.TagsJson,
                x.LastSeenAt,
                x.CreatedAt,
                x.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<MemberDto?> GetAsync(string networkId, string memberId, CancellationToken cancellationToken = default)
    {
        var member = await db.NetworkMembers.FirstOrDefaultAsync(x => x.NetworkId == networkId && x.Id == memberId, cancellationToken);
        return member is null ? null : ToDto(member);
    }

    public async Task<MemberDto> UpsertFromProviderAsync(string networkId, string provider, VirtualMemberInfo memberInfo, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var member = await db.NetworkMembers.FirstOrDefaultAsync(
            x => x.NetworkId == networkId && x.Provider == provider && x.ProviderMemberId == memberInfo.ProviderMemberId,
            cancellationToken);

        if (member is null)
        {
            member = new NetworkMember
            {
                Id = IdGenerator.NewId("member"),
                NetworkId = networkId,
                Provider = provider,
                ProviderMemberId = memberInfo.ProviderMemberId,
                CreatedAt = now
            };
            db.NetworkMembers.Add(member);
        }

        member.Name = memberInfo.Name;
        member.Authorized = memberInfo.Authorized;
        member.ActiveBridge = memberInfo.ActiveBridge;
        member.Online = memberInfo.Online;
        member.IpAssignmentsJson = JsonSerializer.Serialize(memberInfo.IpAssignments);
        member.ProviderRawJson = memberInfo.RawJson;
        member.LastSeenAt = memberInfo.Online ? now : member.LastSeenAt;
        member.UpdatedAt = now;

        await db.SaveChangesAsync(cancellationToken);
        return ToDto(member);
    }

    public async Task<MemberDto> UpdateAsync(string networkId, string memberId, UpdateMemberRequest request, CancellationToken cancellationToken = default)
    {
        var member = await db.NetworkMembers.FirstOrDefaultAsync(x => x.NetworkId == networkId && x.Id == memberId, cancellationToken);
        if (member is null)
        {
            throw new InvalidOperationException("Member not found.");
        }

        var binding = await db.NetworkProviderBindings.FirstOrDefaultAsync(
            x => x.NetworkId == networkId && x.Provider == member.Provider,
            cancellationToken);

        var provider = providers.FirstOrDefault(x => string.Equals(x.ProviderName, member.Provider, StringComparison.OrdinalIgnoreCase));
        if (binding is not null && provider is not null)
        {
            await provider.UpdateMemberAsync(
                binding.ProviderNetworkId,
                member.ProviderMemberId,
                new UpdateVirtualMemberRequest(
                    request.Name,
                    request.Authorized,
                    request.ActiveBridge,
                    request.IpAssignments),
                cancellationToken);
        }

        var now = DateTimeOffset.UtcNow;
        if (request.Name is not null) member.Name = request.Name.Trim();
        if (request.Role is not null) member.Role = request.Role.Trim();
        if (request.Authorized.HasValue) member.Authorized = request.Authorized.Value;
        if (request.ActiveBridge.HasValue) member.ActiveBridge = request.ActiveBridge.Value;
        if (request.IpAssignments is not null) member.IpAssignmentsJson = JsonSerializer.Serialize(request.IpAssignments);
        if (request.Tags is not null) member.TagsJson = JsonSerializer.Serialize(request.Tags);
        member.UpdatedAt = now;

        db.AuditLogs.Add(new AuditLog
        {
            Id = IdGenerator.NewId("audit"),
            Type = "MemberUpdated",
            Actor = "system",
            TargetType = "Member",
            TargetId = member.Id,
            Message = $"更新成员：{member.Name ?? member.ProviderMemberId}",
            CreatedAt = now
        });

        await db.SaveChangesAsync(cancellationToken);
        return ToDto(member);
    }

    public async Task<bool> RemoveAsync(string networkId, string memberId, CancellationToken cancellationToken = default)
    {
        var member = await db.NetworkMembers.FirstOrDefaultAsync(x => x.NetworkId == networkId && x.Id == memberId, cancellationToken);
        if (member is null)
        {
            return false;
        }

        db.NetworkMembers.Remove(member);
        db.AuditLogs.Add(new AuditLog
        {
            Id = IdGenerator.NewId("audit"),
            Type = "MemberRemoved",
            Actor = "system",
            TargetType = "Member",
            TargetId = member.Id,
            Message = $"移除成员：{member.Name ?? member.ProviderMemberId}",
            CreatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static MemberDto ToDto(NetworkMember x) => new(
        x.Id,
        x.NetworkId,
        x.DeviceId,
        x.Provider,
        x.ProviderMemberId,
        x.Name,
        x.Role,
        x.Authorized,
        x.ActiveBridge,
        x.Online,
        x.IpAssignmentsJson,
        x.TagsJson,
        x.LastSeenAt,
        x.CreatedAt,
        x.UpdatedAt);
}

public sealed record UpdateMemberRequest(
    string? Name = null,
    string? Role = null,
    bool? Authorized = null,
    bool? ActiveBridge = null,
    IReadOnlyList<string>? IpAssignments = null,
    IReadOnlyList<string>? Tags = null);

public sealed record MemberDto(
    string Id,
    string NetworkId,
    string? DeviceId,
    string Provider,
    string ProviderMemberId,
    string? Name,
    string Role,
    bool Authorized,
    bool ActiveBridge,
    bool Online,
    string? IpAssignmentsJson,
    string? TagsJson,
    DateTimeOffset? LastSeenAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
