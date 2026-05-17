using HomeMesh.Abstractions.Providers;
using HomeMesh.Domain.Entities;
using HomeMesh.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeMesh.Application.Networks;

public sealed class NetworkService(HomeMeshDbContext db, IEnumerable<ISdwanControllerProvider> providers)
{
    public async Task<IReadOnlyList<NetworkSummaryDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await db.Networks
            .OrderBy(x => x.Name)
            .Select(x => new NetworkSummaryDto(
                x.Id,
                x.HomeId,
                x.Name,
                x.Cidr,
                x.Private,
                x.V4AssignMode,
                x.Status,
                x.CreatedAt,
                x.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<NetworkSummaryDto> CreateAsync(CreateNetworkRequest request, CancellationToken cancellationToken = default)
    {
        var home = await db.Homes.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        if (home is null)
        {
            throw new InvalidOperationException("HomeMesh is not initialized. Please create the first administrator first.");
        }

        var provider = providers.FirstOrDefault(x => string.Equals(x.ProviderName, request.Provider, StringComparison.OrdinalIgnoreCase));
        if (provider is null)
        {
            throw new InvalidOperationException($"Provider '{request.Provider}' was not found.");
        }

        var virtualNetwork = await provider.CreateNetworkAsync(
            new CreateVirtualNetworkRequest(request.Name, request.Cidr, request.Private),
            cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var network = new Network
        {
            Id = IdGenerator.NewId("hmnet"),
            HomeId = home.Id,
            Name = request.Name.Trim(),
            Cidr = request.Cidr,
            Private = request.Private,
            V4AssignMode = true,
            Status = "Created",
            CreatedAt = now,
            UpdatedAt = now
        };

        var binding = new NetworkProviderBinding
        {
            Id = IdGenerator.NewId("npb"),
            NetworkId = network.Id,
            Provider = provider.ProviderName,
            ProviderNetworkId = virtualNetwork.ProviderNetworkId,
            IsPrimary = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Networks.Add(network);
        db.NetworkProviderBindings.Add(binding);
        db.AuditLogs.Add(new AuditLog
        {
            Id = IdGenerator.NewId("audit"),
            HomeId = home.Id,
            Type = "NetworkCreated",
            Actor = "system",
            TargetType = "Network",
            TargetId = network.Id,
            Message = $"创建网络：{network.Name}",
            CreatedAt = now
        });

        await db.SaveChangesAsync(cancellationToken);

        return new NetworkSummaryDto(
            network.Id,
            network.HomeId,
            network.Name,
            network.Cidr,
            network.Private,
            network.V4AssignMode,
            network.Status,
            network.CreatedAt,
            network.UpdatedAt);
    }
}

public sealed record CreateNetworkRequest(
    string Name,
    string Provider = "ZeroTier",
    string? Cidr = null,
    bool Private = true);

public sealed record NetworkSummaryDto(
    string Id,
    string HomeId,
    string Name,
    string? Cidr,
    bool Private,
    bool V4AssignMode,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
