using HomeMesh.Application.Networks;

namespace HomeMesh.WebApi.Endpoints;

public static class NetworkEndpoints
{
    public static IEndpointRouteBuilder MapNetworkEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/networks", async (NetworkService networkService, CancellationToken cancellationToken) =>
        {
            return Results.Ok(await networkService.ListAsync(cancellationToken));
        });

        app.MapPost("/api/networks", async (CreateNetworkRequest request, NetworkService networkService, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(new { error = "Network name is required." });
            }

            try
            {
                var network = await networkService.CreateAsync(request, cancellationToken);
                return Results.Created($"/api/networks/{network.Id}", network);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        app.MapGet("/api/networks/{networkId}", async (string networkId, NetworkService networkService, CancellationToken cancellationToken) =>
        {
            var network = await networkService.GetAsync(networkId, cancellationToken);
            return network is null
                ? Results.NotFound(new { error = "Network not found." })
                : Results.Ok(network);
        });

        app.MapDelete("/api/networks/{networkId}", async (string networkId, NetworkService networkService, CancellationToken cancellationToken) =>
        {
            var deleted = await networkService.DeleteAsync(networkId, cancellationToken);
            return deleted
                ? Results.NoContent()
                : Results.NotFound(new { error = "Network not found." });
        });

        return app;
    }
}
