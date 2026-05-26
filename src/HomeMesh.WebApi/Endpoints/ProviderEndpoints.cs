using System.Text.Json;
using System.Text.Json.Nodes;
using HomeMesh.Abstractions.Providers;
using Microsoft.Extensions.Logging;

namespace HomeMesh.WebApi.Endpoints;

public static class ProviderEndpoints
{
    private static readonly object WriteLock = new();

    public static IEndpointRouteBuilder MapProviderEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/providers", async (IEnumerable<ISdwanControllerProvider> providers, CancellationToken cancellationToken) =>
        {
            var statuses = new List<ProviderHealthStatus>();
            foreach (var provider in providers)
            {
                statuses.Add(await provider.GetStatusAsync(cancellationToken));
            }

            return Results.Ok(statuses);
        });

        app.MapGet("/api/providers/{providerName}/status", async (string providerName, IEnumerable<ISdwanControllerProvider> providers, CancellationToken cancellationToken) =>
        {
            var provider = providers.FirstOrDefault(x => string.Equals(x.ProviderName, providerName, StringComparison.OrdinalIgnoreCase));
            if (provider is null)
            {
                return Results.NotFound(new { error = $"Provider '{providerName}' was not found." });
            }

            return Results.Ok(await provider.GetStatusAsync(cancellationToken));
        });

        app.MapPost("/api/providers/{providerName}/test", async (string providerName, IEnumerable<ISdwanControllerProvider> providers, CancellationToken cancellationToken) =>
        {
            var provider = providers.FirstOrDefault(x => string.Equals(x.ProviderName, providerName, StringComparison.OrdinalIgnoreCase));
            if (provider is null)
            {
                return Results.NotFound(new { error = $"Provider '{providerName}' was not found." });
            }

            return Results.Ok(await provider.GetStatusAsync(cancellationToken));
        });

        app.MapGet("/api/providers/zerotier/config", (IConfiguration configuration) =>
        {
            return Results.Ok(GetCurrentConfig(configuration));
        });

        app.MapPut("/api/providers/zerotier/config", (
            UpdateZeroTierConfigRequest request,
            IConfiguration configuration,
            IWebHostEnvironment environment,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("HomeMesh.WebApi.ProviderConfig");

            if (request.Enabled is null)
            {
                return Results.BadRequest(new { error = "Enabled is required." });
            }

            var port = request.Port ?? 9993;
            if (port is < 1 or > 65535)
            {
                return Results.BadRequest(new { error = "Port must be between 1 and 65535." });
            }

            if (string.IsNullOrWhiteSpace(request.AuthTokenPath))
            {
                return Results.BadRequest(new { error = "AuthTokenPath is required." });
            }

            var config = new ZeroTierConfigDto(
                request.Enabled.Value,
                port,
                request.AuthTokenPath.Trim());

            var appsettingsPath = Path.Combine(environment.ContentRootPath, "appsettings.json");
            try
            {
                SaveConfigToAppSettings(appsettingsPath, config);
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
            {
                logger.LogError(ex, "Failed to save ZeroTier configuration to {AppSettingsPath}.", appsettingsPath);

                return Results.Json(new
                {
                    error = "Failed to save provider configuration.",
                    detail = ex.Message
                }, statusCode: StatusCodes.Status500InternalServerError);
            }

            return Results.Ok(new
            {
                config,
                restartRequired = true,
                message = "Configuration saved to appsettings.json. Restart HomeMesh.WebApi to apply provider runtime settings."
            });
        });

        app.MapPost("/api/providers/zerotier/test-config", async (
            TestZeroTierConfigRequest? request,
            IConfiguration configuration,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger("HomeMesh.WebApi.ProviderDiagnostics");
            var current = GetCurrentConfig(configuration);
            var enabled = request?.Enabled ?? current.Enabled;
            var port = request?.Port ?? current.Port;
            var authTokenPath = string.IsNullOrWhiteSpace(request?.AuthTokenPath) ? current.AuthTokenPath : request!.AuthTokenPath.Trim();

            if (!enabled)
            {
                return Results.Ok(new
                {
                    status = "Disabled",
                    message = "ZeroTier provider is disabled in the current test config.",
                    checkedAt = DateTimeOffset.UtcNow
                });
            }

            if (port is < 1 or > 65535)
            {
                return Results.BadRequest(new { error = "Port must be between 1 and 65535." });
            }

            var resolvedPath = Path.IsPathRooted(authTokenPath) ? authTokenPath : Path.GetFullPath(authTokenPath);
            if (!File.Exists(resolvedPath))
            {
                return Results.BadRequest(new
                {
                    error = "ZeroTier auth token file was not found.",
                    detail = resolvedPath
                });
            }

            try
            {
                var token = (await File.ReadAllTextAsync(resolvedPath, cancellationToken)).Trim();
                var apiBaseUrl = $"http://127.0.0.1:{port}";
                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
                using var requestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri(apiBaseUrl), "/status"));
                requestMessage.Headers.TryAddWithoutValidation("X-ZT1-Auth", token);

                using var response = await httpClient.SendAsync(requestMessage, cancellationToken);
                var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return Results.Json(new
                    {
                        status = "Error",
                        message = $"ZeroTier API responded with {(int)response.StatusCode} {response.ReasonPhrase}.",
                        detail = responseText,
                        checkedAt = DateTimeOffset.UtcNow,
                        config = new ZeroTierConfigDto(enabled, port, authTokenPath)
                    }, statusCode: StatusCodes.Status502BadGateway);
                }

                using var json = JsonDocument.Parse(string.IsNullOrWhiteSpace(responseText) ? "{}" : responseText);
                var root = json.RootElement;
                var online = root.TryGetProperty("online", out var onlineValue) && onlineValue.ValueKind == JsonValueKind.True;
                var address = root.TryGetProperty("address", out var addressValue) ? addressValue.GetString() : null;
                var version = root.TryGetProperty("version", out var versionValue) ? versionValue.GetString() : null;
                var status = online ? "Healthy" : "Warning";

                return Results.Ok(new
                {
                    status,
                    message = $"ZeroTier node {address ?? "unknown"} is {(online ? "online" : "offline")}. Version: {version ?? "unknown"}.",
                    checkedAt = DateTimeOffset.UtcNow,
                    online,
                    nodeAddress = address,
                    version,
                    config = new ZeroTierConfigDto(enabled, port, authTokenPath)
                });
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException)
            {
                logger.LogWarning(
                    ex,
                    "ZeroTier configuration test failed for port {Port} and token path {AuthTokenPath}.",
                    port,
                    authTokenPath);

                return Results.Json(new
                {
                    status = "Error",
                    message = "Failed to connect to ZeroTier local API.",
                    detail = ex.Message,
                    checkedAt = DateTimeOffset.UtcNow,
                    config = new ZeroTierConfigDto(enabled, port, authTokenPath)
                }, statusCode: StatusCodes.Status502BadGateway);
            }
        });

        return app;
    }

    private static ZeroTierConfigDto GetCurrentConfig(IConfiguration configuration)
    {
        var section = configuration.GetSection("Providers:ZeroTier");
        var enabled = section.GetValue<bool?>("Enabled") ?? true;
        var port = section.GetValue<int?>("Port") ?? 9993;
        var authTokenPath = section["AuthTokenPath"] ?? "/var/lib/zerotier-one/authtoken.secret";

        return new ZeroTierConfigDto(enabled, port, authTokenPath);
    }

    private static void SaveConfigToAppSettings(string appsettingsPath, ZeroTierConfigDto config)
    {
        lock (WriteLock)
        {
            var root = File.Exists(appsettingsPath)
                ? JsonNode.Parse(File.ReadAllText(appsettingsPath)) as JsonObject
                : new JsonObject();

            root ??= new JsonObject();
            var providers = root["Providers"] as JsonObject ?? new JsonObject();
            root["Providers"] = providers;

            var zeroTier = providers["ZeroTier"] as JsonObject ?? new JsonObject();
            providers["ZeroTier"] = zeroTier;

            zeroTier["Enabled"] = config.Enabled;
            zeroTier["Port"] = config.Port;
            zeroTier["AuthTokenPath"] = config.AuthTokenPath;

            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(appsettingsPath, root.ToJsonString(options));
        }
    }

    public sealed record ZeroTierConfigDto(bool Enabled, int Port, string AuthTokenPath);

    public sealed record UpdateZeroTierConfigRequest(bool? Enabled, int? Port, string? AuthTokenPath);

    public sealed record TestZeroTierConfigRequest(bool? Enabled, int? Port, string? AuthTokenPath);
}
