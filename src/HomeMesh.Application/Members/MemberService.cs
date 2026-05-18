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

    public async Task<MemberDto> JoinAsync(string networkId, JoinNetworkRequest request, CancellationToken cancellationToken = default)
    {
        var network = await db.Networks.FirstOrDefaultAsync(x => x.Id == networkId, cancellationToken)
            ?? throw new InvalidOperationException("Network not found.");

        var binding = await db.NetworkProviderBindings
            .Where(x => x.NetworkId == networkId)
            .OrderByDescending(x => x.IsPrimary)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Network provider binding not found.");

        var providerMemberId = NormalizeRequired(request.ProviderMemberId, "Provider member id is required.");
        var now = DateTimeOffset.UtcNow;

        var device = await db.Devices.FirstOrDefaultAsync(
            x => x.HomeId == network.HomeId && x.Fingerprint == request.Fingerprint,
            cancellationToken);

        if (device is null)
        {
            device = new Device
            {
                Id = IdGenerator.NewId("dev"),
                HomeId = network.HomeId,
                Name = string.IsNullOrWhiteSpace(request.DeviceName) ? providerMemberId : request.DeviceName.Trim(),
                Platform = string.IsNullOrWhiteSpace(request.Platform) ? "Unknown" : request.Platform.Trim(),
                Fingerprint = request.Fingerprint,
                PublicKey = request.PublicKey,
                LastSeenAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Devices.Add(device);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(request.DeviceName)) device.Name = request.DeviceName.Trim();
            if (!string.IsNullOrWhiteSpace(request.Platform)) device.Platform = request.Platform.Trim();
            if (!string.IsNullOrWhiteSpace(request.PublicKey)) device.PublicKey = request.PublicKey.Trim();
            device.LastSeenAt = now;
            device.UpdatedAt = now;
        }

        var member = await db.NetworkMembers.FirstOrDefaultAsync(
            x => x.NetworkId == networkId && x.Provider == binding.Provider && x.ProviderMemberId == providerMemberId,
            cancellationToken);

        if (member is null)
        {
            member = new NetworkMember
            {
                Id = IdGenerator.NewId("member"),
                NetworkId = networkId,
                Provider = binding.Provider,
                ProviderMemberId = providerMemberId,
                CreatedAt = now
            };
            db.NetworkMembers.Add(member);
        }

        member.DeviceId = device.Id;
        member.Name = device.Name;
        member.Authorized = network.AutoApproveMembers;
        member.Online = true;
        member.IpAssignmentsJson = JsonSerializer.Serialize(request.IpAssignments ?? Array.Empty<string>());
        member.LastSeenAt = now;
        member.UpdatedAt = now;

        if (network.AutoApproveMembers)
        {
            var provider = providers.FirstOrDefault(x => string.Equals(x.ProviderName, binding.Provider, StringComparison.OrdinalIgnoreCase));
            if (provider is not null)
            {
                await provider.UpdateMemberAsync(
                    binding.ProviderNetworkId,
                    providerMemberId,
                    new UpdateVirtualMemberRequest(Authorized: true, IpAssignments: request.IpAssignments),
                    cancellationToken);
            }
        }

        db.AuditLogs.Add(new AuditLog
        {
            Id = IdGenerator.NewId("audit"),
            HomeId = network.HomeId,
            UserId = device.Id,
            Type = "DeviceJoined",
            Actor = device.Name,
            TargetType = "Network",
            TargetId = network.Id,
            Message = $"设备通过 plant 文件加入网络：{device.Name}",
            CreatedAt = now
        });

        await db.SaveChangesAsync(cancellationToken);
        return ToDto(member);
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

    private static string NormalizeRequired(string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(message);
        }

        return value.Trim();
    }
}

public sealed record JoinNetworkRequest(
    string ProviderMemberId,
    string? DeviceName = null,
    string? Platform = null,
    string? Fingerprint = null,
    string? PublicKey = null,
    IReadOnlyList<string>? IpAssignments = null);

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
