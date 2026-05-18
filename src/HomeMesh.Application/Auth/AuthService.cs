using System.Collections.Concurrent;
using HomeMesh.Domain.Entities;
using HomeMesh.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeMesh.Application.Auth;

public sealed class AuthService(HomeMeshDbContext db)
{
    private static readonly ConcurrentDictionary<string, SessionInfo> Sessions = new();

    public async Task<LoginResultDto> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Username == username.Trim(), cancellationToken);
        if (user is null || user.Disabled || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            throw new InvalidOperationException("Invalid username or password.");
        }

        var now = DateTimeOffset.UtcNow;
        user.LastLoginAt = now;
        user.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);

        var sessionId = Guid.NewGuid().ToString("N");
        var expiresAt = now.AddHours(12);
        Sessions[sessionId] = new SessionInfo(user.Id, user.Username, user.Role, expiresAt);

        return new LoginResultDto(sessionId, user.Username, user.Role, expiresAt);
    }

    public AuthUserDto? ValidateSession(string? sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return null;
        }

        if (!Sessions.TryGetValue(sessionId, out var session))
        {
            return null;
        }

        if (session.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            Sessions.TryRemove(sessionId, out _);
            return null;
        }

        return new AuthUserDto(session.UserId, session.Username, session.Role, session.ExpiresAt);
    }

    public void Logout(string? sessionId)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            Sessions.TryRemove(sessionId, out _);
        }
    }

    private sealed record SessionInfo(string UserId, string Username, string Role, DateTimeOffset ExpiresAt);
}

public sealed record LoginRequest(string Username, string Password);

public sealed record LoginResultDto(string SessionId, string Username, string Role, DateTimeOffset ExpiresAt);

public sealed record AuthUserDto(string UserId, string Username, string Role, DateTimeOffset ExpiresAt);
