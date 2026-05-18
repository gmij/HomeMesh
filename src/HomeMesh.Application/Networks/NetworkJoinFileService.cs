using System.Text.Json;
using HomeMesh.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeMesh.Application.Networks;

public sealed class NetworkJoinFileService(HomeMeshDbContext db)
{
    public async Task<NetworkJoinFileDto> GenerateAsync(string networkId, CancellationToken cancellationToken = default)
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

        var payload = new NetworkJoinFilePayload(
            "HomeMesh.NetworkJoinFile.v1",
            network.Id,
            network.Name,
            network.Cidr,
            network.AutoApproveMembers,
            binding.Provider,
            binding.ProviderNetworkId,
            DateTimeOffset.UtcNow);

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        var fileName = $"homemesh-{network.Name.Trim().Replace(' ', '-')}-{binding.Provider}.plant.json";
        return new NetworkJoinFileDto(fileName, "application/json", json);
    }
}

public sealed record NetworkJoinFilePayload(
    string Type,
    string NetworkId,
    string NetworkName,
    string? Cidr,
    bool AutoApproveMembers,
    string Provider,
    string ProviderNetworkId,
    DateTimeOffset GeneratedAt);

public sealed record NetworkJoinFileDto(string FileName, string ContentType, string Content);
