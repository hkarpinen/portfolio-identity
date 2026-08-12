using Application.Dtos;
using Application.Repositories;
using Domain.Aggregates.User;

namespace Identity.Application.Managers.Auth;

public interface ISessionManager
{
    Task<IssuedSession> StartAsync(Guid userId, string? userAgent, string? ipAddress, CancellationToken ct = default);
    Task<IssuedSession?> RefreshAsync(string rawRefreshToken, string? userAgent, string? ipAddress, CancellationToken ct = default);
    Task EndAsync(string rawRefreshToken, CancellationToken ct = default);
    Task<IReadOnlyList<SessionDto>> ListAsync(Guid userId, string? currentRawToken, CancellationToken ct = default);
    Task<bool> RevokeAsync(Guid userId, Guid sessionId, CancellationToken ct = default);
    Task RevokeOthersAsync(Guid userId, string? currentRawToken, CancellationToken ct = default);
}

/// <summary>What the caller must put in cookies: the raw refresh token and when it dies.</summary>
public sealed record IssuedSession(Guid UserId, string RefreshToken, DateTime RefreshExpiresAt);

internal sealed class SessionManager(IRefreshTokenRepository tokens) : ISessionManager
{
    public async Task<IssuedSession> StartAsync(Guid userId, string? userAgent, string? ipAddress, CancellationToken ct = default)
    {
        var (record, raw) = RefreshToken.Issue(userId, userAgent, ipAddress);
        await tokens.AddAsync(record, ct);
        await tokens.SaveChangesAsync(ct);
        return new IssuedSession(userId, raw, record.ExpiresAt);
    }

    /// <summary>
    /// Rotates: the presented token is spent and a successor issued in the same family.
    ///
    /// A token presented after it was already spent means two parties hold the same cookie, so the
    /// entire family is revoked rather than the single link. That logs the real user out too —
    /// which is the point, because the alternative is leaving the thief a working session.
    /// </summary>
    public async Task<IssuedSession?> RefreshAsync(string rawRefreshToken, string? userAgent, string? ipAddress, CancellationToken ct = default)
    {
        var presented = await tokens.GetByHashAsync(RefreshToken.Hash(rawRefreshToken), ct);
        if (presented is null) return null;

        var now = DateTime.UtcNow;

        if (!presented.IsActive(now))
        {
            if (presented.ReplacedById is not null)
            {
                foreach (var relative in await tokens.ListActiveInFamilyAsync(presented.FamilyId, ct))
                    relative.Revoke(now);
                await tokens.SaveChangesAsync(ct);
            }
            return null;
        }

        var (successor, raw) = RefreshToken.Issue(presented.UserId, userAgent, ipAddress, presented.FamilyId);
        presented.ReplaceWith(successor, now);
        successor.Touch(now);

        await tokens.AddAsync(successor, ct);
        await tokens.SaveChangesAsync(ct);

        return new IssuedSession(presented.UserId, raw, successor.ExpiresAt);
    }

    public async Task EndAsync(string rawRefreshToken, CancellationToken ct = default)
    {
        var token = await tokens.GetByHashAsync(RefreshToken.Hash(rawRefreshToken), ct);
        if (token is null) return;

        token.Revoke(DateTime.UtcNow);
        await tokens.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<SessionDto>> ListAsync(Guid userId, string? currentRawToken, CancellationToken ct = default)
    {
        var currentHash = currentRawToken is null ? null : RefreshToken.Hash(currentRawToken);
        var active = await tokens.ListActiveForUserAsync(userId, ct);

        return active
            .Select(t => new SessionDto(
                t.Id,
                t.UserAgent,
                t.IpAddress,
                t.CreatedAt,
                t.LastUsedAt,
                t.ExpiresAt,
                IsCurrent: currentHash is not null && t.TokenHash == currentHash))
            .ToList();
    }

    public async Task<bool> RevokeAsync(Guid userId, Guid sessionId, CancellationToken ct = default)
    {
        var token = await tokens.GetOwnedAsync(userId, sessionId, ct);
        if (token is null) return false;

        token.Revoke(DateTime.UtcNow);
        await tokens.SaveChangesAsync(ct);
        return true;
    }

    public async Task RevokeOthersAsync(Guid userId, string? currentRawToken, CancellationToken ct = default)
    {
        var keepHash = currentRawToken is null ? null : RefreshToken.Hash(currentRawToken);
        var now = DateTime.UtcNow;

        foreach (var session in await tokens.ListActiveForUserAsync(userId, ct))
            if (session.TokenHash != keepHash)
                session.Revoke(now);

        await tokens.SaveChangesAsync(ct);
    }
}
