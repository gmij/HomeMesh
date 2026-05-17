using HomeMesh.Application.NetworkConfig;

namespace HomeMesh.WebApi.Endpoints;

public static class NetworkConfigEndpoints
{
    public static IEndpointRouteBuilder MapNetworkConfigEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/networks/{networkId}/routes", async (
            string networkId,
            RouteService routeService,
            CancellationToken cancellationToken) =>
        {
            return Results.Ok(await routeService.ListAsync(networkId, cancellationToken));
        });

        app.MapPost("/api/networks/{networkId}/routes", async (
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

        app.MapDelete("/api/networks/{networkId}/routes/{routeId}", async (
            string networkId,
            string routeId,
            RouteService routeService,
            CancellationToken cancellationToken) =>
        {
            var deleted = await routeService.DeleteAsync(networkId, routeId, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound(new { error = "Route not found." });
        });

        app.MapGet("/api/networks/{networkId}/ip-pools", async (
            string networkId,
            IpPoolService ipPoolService,
            CancellationToken cancellationToken) =>
        {
            return Results.Ok(await ipPoolService.ListAsync(networkId, cancellationToken));
        });

        app.MapPost("/api/networks/{networkId}/ip-pools", async (
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

        app.MapDelete("/api/networks/{networkId}/ip-pools/{poolId}", async (
            string networkId,
            string poolId,
            IpPoolService ipPoolService,
            CancellationToken cancellationToken) =>
        {
            var deleted = await ipPoolService.DeleteAsync(networkId, poolId, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound(new { error = "IP pool not found." });
        });

        app.MapGet("/api/networks/{networkId}/dns", async (
            string networkId,
            DnsConfigService dnsConfigService,
            CancellationToken cancellationToken) =>
        {
            var config = await dnsConfigService.GetAsync(networkId, cancellationToken);
            return config is null ? Results.NotFound(new { error = "DNS config not found." }) : Results.Ok(config);
        });

        app.MapPut("/api/networks/{networkId}/dns", async (
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
