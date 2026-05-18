using HomeMesh.Application.Auth;
using HomeMesh.Application.Networks;
using HomeMesh.Application.Setup;
using HomeMesh.WebApi.Admin;

namespace HomeMesh.WebApi.Endpoints;

public static class NetworkEndpoints
{
    private const string SessionCookieName = "hm_session";

    public static IEndpointRouteBuilder MapNetworkEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", () => Results.Redirect("/admin"));

        app.MapGet("/admin", () => Results.Content(AdminConsoleHtml.Content, "text/html"));

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
            catch (InvalidOperationException ex)
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
