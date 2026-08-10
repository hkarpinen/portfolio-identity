using Application.Repositories;
using Domain.Aggregates.User;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

internal sealed class RefreshTokenRepository(IdentityDbContext db) : IRefreshTokenRepository
{
    public async Task AddAsync(RefreshToken token, CancellationToken ct = default) =>
        await db.RefreshTokens.AddAsync(token, ct);

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default) =>
        db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task<IReadOnlyList<RefreshToken>> ListActiveForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await db.RefreshTokens
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now)
            .OrderByDescending(t => t.LastUsedAt)
            .ToListAsync(ct);
    }

    public Task<RefreshToken?> GetOwnedAsync(Guid userId, Guid tokenId, CancellationToken ct = default) =>
        db.RefreshTokens.FirstOrDefaultAsync(t => t.Id == tokenId && t.UserId == userId, ct);

    public async Task RevokeFamilyAsync(Guid familyId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        await db.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);
    }

    public async Task RevokeAllForUserAsync(Guid userId, Guid? exceptId = null, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && (exceptId == null || t.Id != exceptId))
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
