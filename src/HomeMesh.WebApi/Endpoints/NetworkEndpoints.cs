using System.Security.Cryptography;
using System.Text;
using HomeMesh.Application.Auth;
using HomeMesh.Application.Diagnostics;
using HomeMesh.Application.Networks;
using HomeMesh.Application.Setup;
using HomeMesh.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HomeMesh.WebApi.Endpoints;

public static class NetworkEndpoints
{
    private const string SessionCookieName = "hm_session";
    private const string DownloadKindPlanet = "planet";
    private const string DownloadKindMoon = "moon";
    private static readonly string DownloadArtifactsDirectory = Path.Combine("/app", "data", "artifacts");
    private static readonly string PlanetFilePath = Path.Combine(DownloadArtifactsDirectory, "planet");

    public static IEndpointRouteBuilder MapNetworkEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/auth/status", async Task<IResult> (SetupService setupService, AuthService authService, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            var initialized = await setupService.IsInitializedAsync(cancellationToken);
            var user = authService.ValidateSession(httpContext.Request.Cookies[SessionCookieName]);
            return Results.Ok(new { initialized, authenticated = user is not null, user });
        });

        app.MapPost("/api/auth/login", async Task<IResult> (
            LoginRequest request,
            AuthService authService,
            HttpContext httpContext,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger("HomeMesh.WebApi.Auth");

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
                logger.LogWarning(
                    ex,
                    "Login failed for username {Username} from {RemoteIp}.",
                    request.Username?.Trim(),
                    httpContext.Connection.RemoteIpAddress?.ToString());

                return Results.Json(new
                {
                    error = ex.Message,
                    detail = ExceptionMetadataSerializer.Summarize(ex)
                }, statusCode: StatusCodes.Status401Unauthorized);
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

        app.MapPost("/api/networks", async Task<IResult> (
            CreateNetworkRequest request,
            NetworkService networkService,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger("HomeMesh.WebApi.Networks");

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
                logger.LogError(
                    ex,
                    "Provider connection failed while creating network {NetworkName} via provider {Provider}.",
                    request.Name,
                    request.Provider);

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

        app.MapGet("/api/networks/{networkId}/access-artifacts", async Task<IResult> (
            string networkId,
            int? expiryDays,
            HomeMeshDbContext db,
            IConfiguration configuration,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var validation = await ValidateNetworkBindingAsync(db, networkId, cancellationToken);
            if (validation is not null)
            {
                return validation;
            }

            var clampedDays = Math.Clamp(expiryDays ?? 7, 1, 30);
            var expiresAt = DateTimeOffset.UtcNow.AddDays(clampedDays);
            var secret = GetDownloadSecret(configuration);

            var planetToken = CreateDownloadToken(networkId, DownloadKindPlanet, expiresAt, secret);
            var moonToken = CreateDownloadToken(networkId, DownloadKindMoon, expiresAt, secret);

            var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
            var planetUrl = $"{baseUrl}/api/public/download/planet?token={Uri.EscapeDataString(planetToken)}";
            var moonUrl = $"{baseUrl}/api/public/download/moon?token={Uri.EscapeDataString(moonToken)}";

            return Results.Ok(new
            {
                networkId,
                expiresAt,
                expiryDays = clampedDays,
                planetUrl,
                moonUrl
            });
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

            if (!File.Exists(PlanetFilePath))
            {
                return Results.NotFound(new { error = "Planet file not found. Please wait for container initialization or regenerate it." });
            }

            return Results.File(
                await File.ReadAllBytesAsync(PlanetFilePath, cancellationToken),
                "application/octet-stream",
                "planet");
        });

        app.MapGet("/api/networks/{networkId}/moon-file", async Task<IResult> (string networkId, HomeMeshDbContext db, CancellationToken cancellationToken) =>
        {
            var validation = await ValidateNetworkBindingAsync(db, networkId, cancellationToken);
            if (validation is not null)
            {
                return validation;
            }

            if (!Directory.Exists(DownloadArtifactsDirectory))
            {
                return Results.NotFound(new { error = "Moon file directory was not found." });
            }

            var moonFilePath = GetLatestMoonFilePath();

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

        app.MapGet("/api/public/download/planet", async Task<IResult> (string token, HomeMeshDbContext db, IConfiguration configuration, CancellationToken cancellationToken) =>
        {
            if (!TryValidateDownloadToken(token, DownloadKindPlanet, configuration, out var payload, out var error))
            {
                return Results.Unauthorized();
            }

            var validation = await ValidateNetworkBindingAsync(db, payload.NetworkId, cancellationToken);
            if (validation is not null)
            {
                return validation;
            }

            if (!File.Exists(PlanetFilePath))
            {
                return Results.NotFound(new { error = "Planet file not found. Please wait for container initialization or regenerate it." });
            }

            return Results.File(
                await File.ReadAllBytesAsync(PlanetFilePath, cancellationToken),
                "application/octet-stream",
                "planet");
        });

        app.MapGet("/api/public/download/moon", async Task<IResult> (string token, HomeMeshDbContext db, IConfiguration configuration, CancellationToken cancellationToken) =>
        {
            if (!TryValidateDownloadToken(token, DownloadKindMoon, configuration, out var payload, out var error))
            {
                return Results.Unauthorized();
            }

            var validation = await ValidateNetworkBindingAsync(db, payload.NetworkId, cancellationToken);
            if (validation is not null)
            {
                return validation;
            }

            if (!Directory.Exists(DownloadArtifactsDirectory))
            {
                return Results.NotFound(new { error = "Moon file directory was not found." });
            }

            var moonFilePath = GetLatestMoonFilePath();

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

    private static string? GetLatestMoonFilePath()
    {
        return Directory
            .GetFiles(DownloadArtifactsDirectory, "*.moon")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    private static async Task<IResult?> ValidateNetworkBindingAsync(HomeMeshDbContext db, string networkId, CancellationToken cancellationToken)
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

        return null;
    }

    private static string CreateDownloadToken(string networkId, string kind, DateTimeOffset expiresAt, string secret)
    {
        var payload = $"{networkId}|{kind}|{expiresAt.ToUnixTimeSeconds()}";
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var signatureBytes = ComputeHmac(payloadBytes, secret);
        return $"{ToBase64Url(payloadBytes)}.{ToBase64Url(signatureBytes)}";
    }

    private static bool TryValidateDownloadToken(
        string token,
        string expectedKind,
        IConfiguration configuration,
        out DownloadTokenPayload payload,
        out string error)
    {
        payload = default;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(token))
        {
            error = "Missing token.";
            return false;
        }

        var parts = token.Split('.', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            error = "Invalid token format.";
            return false;
        }

        byte[] payloadBytes;
        byte[] signatureBytes;
        try
        {
            payloadBytes = FromBase64Url(parts[0]);
            signatureBytes = FromBase64Url(parts[1]);
        }
        catch
        {
            error = "Invalid token encoding.";
            return false;
        }

        var expectedSignature = ComputeHmac(payloadBytes, GetDownloadSecret(configuration));
        if (!CryptographicOperations.FixedTimeEquals(signatureBytes, expectedSignature))
        {
            error = "Invalid token signature.";
            return false;
        }

        var payloadText = Encoding.UTF8.GetString(payloadBytes);
        var payloadParts = payloadText.Split('|', 3, StringSplitOptions.None);
        if (payloadParts.Length != 3 || !long.TryParse(payloadParts[2], out var expiresUnix))
        {
            error = "Invalid token payload.";
            return false;
        }

        if (!string.Equals(payloadParts[1], expectedKind, StringComparison.OrdinalIgnoreCase))
        {
            error = "Token type mismatch.";
            return false;
        }

        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expiresUnix);
        if (expiresAt <= DateTimeOffset.UtcNow)
        {
            error = "Token expired.";
            return false;
        }

        payload = new DownloadTokenPayload(payloadParts[0], payloadParts[1], expiresAt);
        return true;
    }

    private static string GetDownloadSecret(IConfiguration configuration)
    {
        return configuration["PublicDownloadSecret"]
            ?? Environment.GetEnvironmentVariable("PUBLIC_DOWNLOAD_SECRET")
            ?? "homemesh-public-download-secret-change-me";
    }

    private static byte[] ComputeHmac(byte[] payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return hmac.ComputeHash(payload);
    }

    private static string ToBase64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] FromBase64Url(string value)
    {
        var base64 = value
            .Replace('-', '+')
            .Replace('_', '/');

        switch (base64.Length % 4)
        {
            case 2:
                base64 += "==";
                break;
            case 3:
                base64 += "=";
                break;
        }

        return Convert.FromBase64String(base64);
    }

    private readonly record struct DownloadTokenPayload(string NetworkId, string Kind, DateTimeOffset ExpiresAt);
}
