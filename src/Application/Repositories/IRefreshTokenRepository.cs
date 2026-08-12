using Domain.Aggregates.User;

namespace Application.Repositories;

/// <summary>
/// Persistence only. Every state change goes through <see cref="RefreshToken"/> itself — these
/// return the aggregates so the caller can revoke them, rather than reaching past the aggregate
/// with a bulk update and setting RevokedAt behind its back.
/// </summary>
public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default);

    /// <summary>Looked up by hash — the raw value is never stored, so this is the only way in.</summary>
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>Scoped to the owner: a guessed id belonging to somebody else comes back null.</summary>
    Task<RefreshToken?> GetOwnedAsync(Guid userId, Guid tokenId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RefreshToken>> ListActiveForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Everything descended from one sign-in, for when a spent token is presented again.</summary>
    Task<IReadOnlyList<RefreshToken>> ListActiveInFamilyAsync(Guid familyId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
