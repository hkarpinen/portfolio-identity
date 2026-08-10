using Domain.Aggregates.User;

namespace Application.Repositories;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default);

    /// <summary>Looked up by hash — the raw value is never stored, so this is the only way in.</summary>
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RefreshToken>> ListActiveForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Tracked, and scoped to the owner: a guessed id belonging to somebody else comes
    /// back null rather than revoking their session.</summary>
    Task<RefreshToken?> GetOwnedAsync(Guid userId, Guid tokenId, CancellationToken cancellationToken = default);

    /// <summary>Revokes every token descended from one sign-in. Used when a spent token is
    /// presented again, which means the cookie leaked.</summary>
    Task RevokeFamilyAsync(Guid familyId, CancellationToken cancellationToken = default);

    Task RevokeAllForUserAsync(Guid userId, Guid? exceptId = null, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
