using HomeMesh.Domain.Entities;
using HomeMesh.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeMesh.Application.Setup;

public sealed class SetupService(HomeMeshDbContext db)
{
    public async Task<bool> IsInitializedAsync(CancellationToken cancellationToken = default)
    {
        return await db.Users.AnyAsync(cancellationToken);
    }

    public async Task InitializeAdminAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        if (await IsInitializedAsync(cancellationToken))
        {
            throw new InvalidOperationException("HomeMesh has already been initialized.");
        }

        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            Id = IdGenerator.NewId("user"),
            Username = username.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = "Admin",
            CreatedAt = now,
            UpdatedAt = now
        };

        var home = new Home
        {
            Id = IdGenerator.NewId("home"),
            Name = "默认家庭",
            OwnerUserId = user.Id,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Users.Add(user);
        db.Homes.Add(home);
        db.AuditLogs.Add(new AuditLog
        {
            Id = IdGenerator.NewId("audit"),
            HomeId = home.Id,
            UserId = user.Id,
            Type = "SetupInitialized",
            Actor = username,
            TargetType = "Home",
            TargetId = home.Id,
            Message = "初始化 HomeMesh 管理员和默认家庭空间",
            CreatedAt = now
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}

public static class IdGenerator
{
    public static string NewId(string prefix)
    {
        return $"{prefix}_{Guid.NewGuid():N}";
    }
}
