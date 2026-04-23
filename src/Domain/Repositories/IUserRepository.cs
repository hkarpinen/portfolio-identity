using Domain.Aggregates.User;

namespace Domain.Repositories;

public interface IUserRepository
{
    Task<AppUser?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default);
    Task<AppUser?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);
    Task SaveAsync(AppUser user, CancellationToken cancellationToken = default);

    // Password & account management (delegated to ASP.NET Identity in infrastructure)
    Task<(bool Succeeded, string? Error)> CreateWithPasswordAsync(AppUser user, string password, CancellationToken cancellationToken = default);
    Task<string> GenerateEmailConfirmationTokenAsync(AppUser user, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> ConfirmEmailAsync(AppUser user, string token, CancellationToken cancellationToken = default);
    Task ResetAuthenticatorKeyAsync(AppUser user, CancellationToken cancellationToken = default);
    Task<string?> GetAuthenticatorKeyAsync(AppUser user, CancellationToken cancellationToken = default);
    Task<bool> VerifyTwoFactorTokenAsync(AppUser user, string code, CancellationToken cancellationToken = default);
    Task SetTwoFactorEnabledAsync(AppUser user, bool enabled, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> UpdateAsync(AppUser user, CancellationToken cancellationToken = default);
}
