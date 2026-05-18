using HomeMesh.Application.Sync;
using HomeMesh.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeMesh.WebApi.Endpoints;

public static class SyncEndpoints
{
    public static IEndpointRouteBuilder MapSyncEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/networks/{networkId}/sync/members", async Task<IResult> (
            string networkId,
            NetworkSyncService syncService,
            CancellationToken cancellationToken) =>
        {
            var result = await syncService.SyncMembersAsync(networkId, cancellationToken);
            return Results.Ok(result);
        });

        app.MapGet("/api/networks/{networkId}/sync-state", async Task<IResult> (
            string networkId,
            HomeMeshDbContext db,
            CancellationToken cancellationToken) =>
        {
            var states = await db.ProviderSyncStates
                .Where(x => x.ResourceId == networkId)
                .OrderBy(x => x.ResourceType)
                .ToListAsync(cancellationToken);

            return Results.Ok(states);
        });

        return app;
    }
}
