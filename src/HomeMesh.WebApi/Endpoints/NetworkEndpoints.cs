using System.Text.Json;
using HomeMesh.Application.Auth;
using HomeMesh.Application.Networks;
using HomeMesh.Application.Setup;
using HomeMesh.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeMesh.WebApi.Endpoints;

public static class NetworkEndpoints
{
    private const string SessionCookieName = "hm_session";

    public static IEndpointRouteBuilder MapNetworkEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/auth/status", async Task<IResult> (SetupService setupService, AuthService authService, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            var initialized = await setupService.IsInitializedAsync(cancellationToken);
            var user = authService.ValidateSession(httpContext.Request.Cookies[SessionCookieName]);
            return Results.Ok(new { initialized, authenticated = user is not null, user });
        });

        app.MapPost("/api/auth/login", async Task<IResult> (LoginRequest request, AuthService authService, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await authService.LoginAsync(request.Username, request.Password, cancellationToken);
                httpContext.Response.Cookies.Append(SessionCookieName, result.SessionId, new CookieOptions
                {
                    HttpOnly = true,
                    SameSite = SameSiteMode.Strict,
                    Secure = httpContext.Request.IsHttps,
                    Expires = result.ExpiresAt
                });

                return Results.Ok(new { result.Username, result.Role, result.ExpiresAt });
            }
            catch (InvalidOperationException)
            {
                return Results.Unauthorized();
            }
        });

        app.MapPost("/api/auth/logout", (AuthService authService, HttpContext httpContext) =>
        {
            authService.Logout(httpContext.Request.Cookies[SessionCookieName]);
            httpContext.Response.Cookies.Delete(SessionCookieName);
            return Results.Ok(new { loggedOut = true });
        });

        app.MapGet("/api/dashboard/summary", async Task<IResult> (HomeMeshDbContext db, CancellationToken cancellationToken) =>
        {
            var lastAuditAt = (await db.AuditLogs
                    .Select(x => x.CreatedAt)
                    .ToListAsync(cancellationToken))
                .OrderByDescending(x => x)
                .FirstOrDefault();

            var summary = new
            {
                networkCount = await db.Networks.CountAsync(cancellationToken),
                memberCount = await db.NetworkMembers.CountAsync(cancellationToken),
                authorizedMemberCount = await db.NetworkMembers.CountAsync(x => x.Authorized, cancellationToken),
                onlineMemberCount = await db.NetworkMembers.CountAsync(x => x.Online, cancellationToken),
                routeCount = await db.Routes.CountAsync(cancellationToken),
                ipPoolCount = await db.IpPools.CountAsync(cancellationToken),
                errorSyncCount = await db.ProviderSyncStates.CountAsync(x => x.Status == "Error", cancellationToken),
                lastAuditAt
            };

            return Results.Ok(summary);
        });

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
            catch (Exception ex) when (IsProviderConnectionException(ex))
            {
                return Results.Json(new
                {
                    error = "Provider connection failed. Check that ZeroTier service is running and the auth token path is correct.",
                    detail = ex.Message
                }, statusCode: StatusCodes.Status502BadGateway);
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

        app.MapGet("/api/networks/{networkId}/plant-file", async Task<IResult> (string networkId, HomeMeshDbContext db, CancellationToken cancellationToken) =>
        {
            var network = await db.Networks.FirstOrDefaultAsync(x => x.Id == networkId, cancellationToken);
            if (network is null)
            {
                return Results.NotFound(new { error = "Network not found." });
            }

            var binding = await db.NetworkProviderBindings
                .Where(x => x.NetworkId == networkId)
                .OrderByDescending(x => x.IsPrimary)
                .FirstOrDefaultAsync(cancellationToken);

            if (binding is null)
            {
                return Results.NotFound(new { error = "Network provider binding not found." });
            }

            var planetPath = "/app/dist/planet";
            if (!File.Exists(planetPath))
            {
                return Results.NotFound(new { error = "Planet file not found. Please wait for container initialization or regenerate it." });
            }

            return Results.File(
                await File.ReadAllBytesAsync(planetPath, cancellationToken),
                "application/octet-stream",
                "planet");
        });

        app.MapGet("/api/networks/{networkId}/moon-file", async Task<IResult> (string networkId, HomeMeshDbContext db, CancellationToken cancellationToken) =>
        {
            var network = await db.Networks.FirstOrDefaultAsync(x => x.Id == networkId, cancellationToken);
            if (network is null)
            {
                return Results.NotFound(new { error = "Network not found." });
            }

            var binding = await db.NetworkProviderBindings
                .Where(x => x.NetworkId == networkId)
                .OrderByDescending(x => x.IsPrimary)
                .FirstOrDefaultAsync(cancellationToken);

            if (binding is null)
            {
                return Results.NotFound(new { error = "Network provider binding not found." });
            }

            var moonDirectory = "/app/dist";
            if (!Directory.Exists(moonDirectory))
            {
                return Results.NotFound(new { error = "Moon file directory was not found." });
            }

            var moonFilePath = Directory
                .GetFiles(moonDirectory, "*.moon")
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(moonFilePath))
            {
                return Results.NotFound(new { error = "Moon file not found. Please wait for container initialization or regenerate it." });
            }

            var moonFileName = Path.GetFileName(moonFilePath);
            return Results.File(
                await File.ReadAllBytesAsync(moonFilePath, cancellationToken),
                "application/octet-stream",
                moonFileName);
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

    private static bool IsProviderConnectionException(Exception exception)
    {
        return exception is HttpRequestException or IOException or TaskCanceledException;
    }
}
