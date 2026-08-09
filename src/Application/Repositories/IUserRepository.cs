using Domain.Aggregates.User;

namespace Application.Repositories;

public interface IUserRepository
{
    Task<AppUser?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default);
    Task<AppUser?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);
    Task SaveAsync(AppUser user, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> CreateWithPasswordAsync(AppUser user, string password, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> CreateDemoAsync(AppUser user, CancellationToken cancellationToken = default);
    Task<string> GenerateEmailConfirmationTokenAsync(AppUser user, CancellationToken cancellationToken = default);
    Task QueueConfirmationEmailAsync(AppUser user, string token, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> ConfirmEmailAsync(AppUser user, string token, CancellationToken cancellationToken = default);
    Task<string> GeneratePasswordResetTokenAsync(AppUser user, CancellationToken cancellationToken = default);
    Task QueuePasswordResetEmailAsync(AppUser user, string token, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> ResetPasswordAsync(AppUser user, string token, string newPassword, CancellationToken cancellationToken = default);
    Task ResetAuthenticatorKeyAsync(AppUser user, CancellationToken cancellationToken = default);
    Task<string?> GetAuthenticatorKeyAsync(AppUser user, CancellationToken cancellationToken = default);
    Task<bool> VerifyTwoFactorTokenAsync(AppUser user, string code, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> UpdateAsync(AppUser user, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AppUser>> GetExpiredDemoUsersAsync(CancellationToken cancellationToken = default);
    Task<bool> CheckPasswordAsync(AppUser user, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Change the password, verifying the current one. Distinct from
    /// <c>ResetPasswordAsync</c>, which is the emailed-token flow for someone
    /// who cannot sign in.
    /// </summary>
    Task<(bool Succeeded, string? Error)> ChangePasswordAsync(AppUser user, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> ChangeEmailAsync(AppUser user, string newEmail, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GenerateRecoveryCodesAsync(AppUser user, int count = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// How many recovery codes the user has left. Reading the count must never
    /// mint a new set — the Security screen shows this on every visit.
    /// </summary>
    Task<int> CountRecoveryCodesAsync(AppUser user, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(string Provider, string ProviderKey)>> GetExternalLoginsAsync(AppUser user, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> RemoveExternalLoginAsync(AppUser user, string provider, CancellationToken cancellationToken = default);
    Task<int> CountPasswordsAndLoginsAsync(AppUser user, CancellationToken cancellationToken = default);
}
