using HomeMesh.Abstractions.Providers;
using HomeMesh.Application;
using HomeMesh.Application.Auth;
using HomeMesh.Application.Setup;
using HomeMesh.Infrastructure;
using HomeMesh.Infrastructure.Persistence;
using HomeMesh.Protocol.ZeroTier;
using HomeMesh.WebApi.Endpoints;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHomeMeshInfrastructure(builder.Configuration);
builder.Services.AddZeroTierProvider(builder.Configuration);
builder.Services.AddHomeMeshApplication();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HomeMeshDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.Use(async (context, next) =>
{
    if (!RequiresAdminSession(context.Request.Path))
    {
        await next();
        return;
    }

    var authService = context.RequestServices.GetRequiredService<AuthService>();
    var user = authService.ValidateSession(context.Request.Cookies["hm_session"]);
    if (user is null)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
        return;
    }

    await next();
});

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "HomeMesh.Controller",
    checkedAt = DateTimeOffset.UtcNow
}));

app.MapGet("/api/setup/status", async (SetupService setupService, CancellationToken cancellationToken) =>
{
    var initialized = await setupService.IsInitializedAsync(cancellationToken);
    return Results.Ok(new { initialized });
});

app.MapPost("/api/setup/admin", async (SetupAdminRequest request, SetupService setupService, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { error = "Username and password are required." });
    }

    try
    {
        await setupService.InitializeAdminAsync(request.Username, request.Password, cancellationToken);
        return Results.Ok(new { initialized = true });
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
});

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

app.MapNetworkEndpoints();
app.MapNetworkConfigEndpoints();
app.MapMemberEndpoints();

app.MapGet("/api/audit-logs", async (HomeMeshDbContext db, CancellationToken cancellationToken) =>
{
    var logs = await db.AuditLogs
        .OrderByDescending(x => x.CreatedAt)
        .Take(100)
        .ToListAsync(cancellationToken);

    return Results.Ok(logs);
});

app.Run();

static bool RequiresAdminSession(PathString path)
{
    if (!path.StartsWithSegments("/api")) return false;
    if (path.StartsWithSegments("/api/auth")) return false;
    if (path.StartsWithSegments("/api/setup")) return false;
    if (path.StartsWithSegments("/api/join")) return false;
    return true;
}

public sealed record SetupAdminRequest(string Username, string Password);
