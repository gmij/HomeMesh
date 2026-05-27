using HomeMesh.Application.NetworkConfig;
using HomeMesh.Application.Sync;
using HomeMesh.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeMesh.WebApi.Endpoints;

public static class NetworkConfigEndpoints
{
    public static IEndpointRouteBuilder MapNetworkConfigEndpoints(this IEndpointRouteBuilder app)
    {
        static async Task<IResult> DeleteRouteAsync(
            string networkId,
            string routeId,
            RouteService routeService,
            CancellationToken cancellationToken)
        {
            var deleted = await routeService.DeleteAsync(networkId, routeId, cancellationToken);
            if (!deleted)
            {
                return Results.NotFound(new { error = "Route not found." });
            }

            return Results.NoContent();
        }

        static async Task<IResult> DeleteIpPoolAsync(
            string networkId,
            string poolId,
            IpPoolService ipPoolService,
            CancellationToken cancellationToken)
        {
            var deleted = await ipPoolService.DeleteAsync(networkId, poolId, cancellationToken);
            if (!deleted)
            {
                return Results.NotFound(new { error = "IP pool not found." });
            }

            return Results.NoContent();
        }

        app.MapPost("/api/networks/{networkId}/easy-setup", async Task<IResult> (
            string networkId,
            EasySetupRequest request,
            EasySetupService easySetupService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await easySetupService.ApplyAsync(networkId, request, cancellationToken);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        app.MapPost("/api/networks/{networkId}/config/sync", async Task<IResult> (
            string networkId,
            NetworkConfigSyncService syncService,
            CancellationToken cancellationToken) =>
        {
            var result = await syncService.SyncConfigAsync(networkId, cancellationToken);
            return Results.Ok(result);
        });

        app.MapGet("/api/networks/{networkId}/config/sync-state", async Task<IResult> (
            string networkId,
            HomeMeshDbContext db,
            CancellationToken cancellationToken) =>
        {
            var states = await db.ProviderSyncStates
                .Where(x => x.ResourceId == networkId && x.ResourceType == "NetworkConfig")
                .OrderBy(x => x.Provider)
                .ToListAsync(cancellationToken);

            return Results.Ok(states);
        });

        app.MapGet("/api/networks/{networkId}/routes", async Task<IResult> (
            string networkId,
            RouteService routeService,
            CancellationToken cancellationToken) =>
        {
            return Results.Ok(await routeService.ListAsync(networkId, cancellationToken));
        });

        app.MapPost("/api/networks/{networkId}/routes", async Task<IResult> (
            string networkId,
            CreateRouteRequest request,
            RouteService routeService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var route = await routeService.CreateAsync(networkId, request, cancellationToken);
                return Results.Created($"/api/networks/{networkId}/routes/{route.Id}", route);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        app.MapDelete("/api/networks/{networkId}/routes/{routeId}", DeleteRouteAsync);
        app.MapPost("/api/networks/{networkId}/routes/{routeId}/delete", DeleteRouteAsync);

        app.MapGet("/api/networks/{networkId}/ip-pools", async Task<IResult> (
            string networkId,
            IpPoolService ipPoolService,
            CancellationToken cancellationToken) =>
        {
            return Results.Ok(await ipPoolService.ListAsync(networkId, cancellationToken));
        });

        app.MapPost("/api/networks/{networkId}/ip-pools", async Task<IResult> (
            string networkId,
            CreateIpPoolRequest request,
            IpPoolService ipPoolService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var pool = await ipPoolService.CreateAsync(networkId, request, cancellationToken);
                return Results.Created($"/api/networks/{networkId}/ip-pools/{pool.Id}", pool);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        app.MapDelete("/api/networks/{networkId}/ip-pools/{poolId}", DeleteIpPoolAsync);
        app.MapPost("/api/networks/{networkId}/ip-pools/{poolId}/delete", DeleteIpPoolAsync);

        app.MapGet("/api/networks/{networkId}/dns", async Task<IResult> (
            string networkId,
            DnsConfigService dnsConfigService,
            CancellationToken cancellationToken) =>
        {
            var config = await dnsConfigService.GetAsync(networkId, cancellationToken);
            if (config is null)
            {
                return Results.NotFound(new { error = "DNS config not found." });
            }

            return Results.Ok(config);
        });

        app.MapPut("/api/networks/{networkId}/dns", async Task<IResult> (
            string networkId,
            UpsertDnsConfigRequest request,
            DnsConfigService dnsConfigService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var config = await dnsConfigService.UpsertAsync(networkId, request, cancellationToken);
                return Results.Ok(config);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        return app;
    }
}
