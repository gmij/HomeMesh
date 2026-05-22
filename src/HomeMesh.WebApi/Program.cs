using HomeMesh.Application;
using HomeMesh.Application.Auth;
using HomeMesh.Application.Setup;
using HomeMesh.Infrastructure;
using HomeMesh.Infrastructure.Persistence;
using HomeMesh.Protocol.ZeroTier;
using HomeMesh.WebApi.Endpoints;
using Microsoft.EntityFrameworkCore;
using Serilog;

const string MissingAdminConsoleHtml = """
<!doctype html>
<html lang="zh-CN">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>HomeMesh Admin Console</title>
  <style>
    body{margin:0;font-family:system-ui,-apple-system,BlinkMacSystemFont,"Segoe UI",sans-serif;background:#f3f6fb;color:#0f172a;display:grid;place-items:center;min-height:100vh;padding:24px}
    main{max-width:720px;background:#fff;border:1px solid #dbe4f1;border-radius:20px;padding:28px;box-shadow:0 20px 48px rgba(15,23,42,.08)}
    h1{margin:0 0 12px;font-size:28px}
    p{margin:0 0 12px;line-height:1.7;color:#475569}
    code{padding:2px 8px;border-radius:8px;background:#eff6ff;color:#1d4ed8}
  </style>
</head>
<body>
  <main>
    <h1>HomeMesh Admin Console 尚未构建</h1>
    <p>新的前端已经切换为 Vue + Ant Design Vue。</p>
    <p>请先在 <code>src/HomeMesh.AdminConsole</code> 下执行 <code>npm install</code> 与 <code>npm run build</code>，或直接使用发布流程自动构建。</p>
  </main>
</body>
</html>
""";

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/admin/{*path}", () => Results.Redirect("/"));

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

app.MapProviderEndpoints();
app.MapNetworkEndpoints();
app.MapNetworkConfigEndpoints();
app.MapMemberEndpoints();

app.MapGet("/api/audit-logs", async (HomeMeshDbContext db, CancellationToken cancellationToken) =>
{
    var logs = (await db.AuditLogs
            .ToListAsync(cancellationToken))
        .OrderByDescending(x => x.CreatedAt)
        .Take(100)
        .ToList();

    return Results.Ok(logs);
});

app.MapFallback((IWebHostEnvironment environment) =>
{
    var webRootPath = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
    var indexPath = Path.Combine(webRootPath, "index.html");

    return File.Exists(indexPath)
        ? Results.File(indexPath, "text/html; charset=utf-8")
        : Results.Content(MissingAdminConsoleHtml, "text/html; charset=utf-8");
});

app.Run();

static bool RequiresAdminSession(PathString path)
{
    if (!path.StartsWithSegments("/api")) return false;
    if (path.StartsWithSegments("/api/auth")) return false;
    if (path.StartsWithSegments("/api/setup")) return false;
    if (path.StartsWithSegments("/api/join")) return false;
    if (path.StartsWithSegments("/api/public")) return false;
    return true;
}

public sealed record SetupAdminRequest(string Username, string Password);
