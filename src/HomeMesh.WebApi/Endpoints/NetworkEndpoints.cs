using HomeMesh.Application.Networks;
using HomeMesh.WebApi.Admin;

namespace HomeMesh.WebApi.Endpoints;

public static class NetworkEndpoints
{
    public static IEndpointRouteBuilder MapNetworkEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", () => Results.Redirect("/admin"));

        app.MapGet("/admin", () => Results.Content(AdminConsoleHtml.Content, "text/html"));

        app.MapGet("/api/networks", async Task<IResult> (NetworkService networkService, CancellationToken cancellationToken) =>
        {
            return Results.Ok(await networkService.ListAsync(cancellationToken));
        });

        app.MapPost("/api/networks", async Task<IResult> (CreateNetworkRequest request, NetworkService networkService, CancellationToken cancellationToken) =>
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

        app.MapGet("/api/networks/{networkId}", async Task<IResult> (string networkId, NetworkService networkService, CancellationToken cancellationToken) =>
        {
            var network = await networkService.GetAsync(networkId, cancellationToken);
            if (network is null)
            {
                return Results.NotFound(new { error = "Network not found." });
            }

            return Results.Ok(network);
        });

        app.MapDelete("/api/networks/{networkId}", async Task<IResult> (string networkId, NetworkService networkService, CancellationToken cancellationToken) =>
        {
            var deleted = await networkService.DeleteAsync(networkId, cancellationToken);
            if (!deleted)
            {
                return Results.NotFound(new { error = "Network not found." });
            }

            return Results.NoContent();
        });

        return app;
    }
}
