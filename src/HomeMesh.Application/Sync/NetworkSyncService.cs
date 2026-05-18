using HomeMesh.Abstractions.Providers;
using HomeMesh.Application.Members;
using HomeMesh.Application.Setup;
using HomeMesh.Domain.Entities;
using HomeMesh.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeMesh.Application.Sync;

public sealed class NetworkSyncService(
    HomeMeshDbContext db,
    IEnumerable<ISdwanControllerProvider> providers,
    MemberService memberService)
{
    public async Task<MemberSyncResultDto> SyncMembersAsync(string networkId, CancellationToken cancellationToken = default)
    {
        var network = await db.Networks.FirstOrDefaultAsync(x => x.Id == networkId, cancellationToken);
        if (network is null)
        {
            throw new InvalidOperationException("Network not found.");
        }

        var binding = await db.NetworkProviderBindings
            .Where(x => x.NetworkId == networkId)
            .OrderByDescending(x => x.IsPrimary)
            .FirstOrDefaultAsync(cancellationToken);

        if (binding is null)
        {
            throw new InvalidOperationException("Network provider binding not found.");
        }

        var provider = providers.FirstOrDefault(x => string.Equals(x.ProviderName, binding.Provider, StringComparison.OrdinalIgnoreCase));
        if (provider is null)
        {
            throw new InvalidOperationException($"Provider '{binding.Provider}' was not found.");
        }

        var now = DateTimeOffset.UtcNow;
        var state = await FindOrCreateSyncStateAsync(binding.Provider, "Members", networkId, cancellationToken);

        try
        {
            var providerMembers = await provider.ListMembersAsync(binding.ProviderNetworkId, cancellationToken);
            var synced = 0;
            var autoApproved = 0;

            foreach (var providerMember in providerMembers)
            {
                var memberInfo = providerMember;
                if (network.AutoApproveMembers && !providerMember.Authorized)
                {
                    memberInfo = await provider.UpdateMemberAsync(
                        binding.ProviderNetworkId,
                        providerMember.ProviderMemberId,
                        new UpdateVirtualMemberRequest(Authorized: true),
                        cancellationToken);
                    autoApproved++;
                }

                await memberService.UpsertFromProviderAsync(networkId, binding.Provider, memberInfo, cancellationToken);
                synced++;
            }

            state.Status = "Healthy";
            state.LastError = null;
            state.LastSyncAt = now;

            db.AuditLogs.Add(new AuditLog
            {
                Id = IdGenerator.NewId("audit"),
                HomeId = network.HomeId,
                Type = "MembersSynced",
                Actor = "system",
                TargetType = "Network",
                TargetId = network.Id,
                Message = $"同步网络成员：{network.Name}，共 {synced} 个成员，自动授权 {autoApproved} 个",
                CreatedAt = now
            });

            await db.SaveChangesAsync(cancellationToken);

            return new MemberSyncResultDto(
                network.Id,
                binding.Provider,
                binding.ProviderNetworkId,
                synced,
                autoApproved,
                "Healthy",
                null,
                now);
        }
        catch (Exception ex)
        {
            state.Status = "Error";
            state.LastError = ex.Message;
            state.LastSyncAt = now;
            await db.SaveChangesAsync(cancellationToken);

            return new MemberSyncResultDto(
                network.Id,
                binding.Provider,
                binding.ProviderNetworkId,
                0,
                0,
                "Error",
                ex.Message,
                now);
        }
    }

    private async Task<ProviderSyncState> FindOrCreateSyncStateAsync(
        string provider,
        string resourceType,
        string resourceId,
        CancellationToken cancellationToken)
    {
        var state = await db.ProviderSyncStates.FirstOrDefaultAsync(
            x => x.Provider == provider && x.ResourceType == resourceType && x.ResourceId == resourceId,
            cancellationToken);

        if (state is not null)
        {
            return state;
        }

        state = new ProviderSyncState
        {
            Id = IdGenerator.NewId("sync"),
            Provider = provider,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Status = "Pending"
        };

        db.ProviderSyncStates.Add(state);
        return state;
    }
}

public sealed record MemberSyncResultDto(
    string NetworkId,
    string Provider,
    string ProviderNetworkId,
    int SyncedMemberCount,
    int AutoApprovedMemberCount,
    string Status,
    string? Error,
    DateTimeOffset SyncedAt);
