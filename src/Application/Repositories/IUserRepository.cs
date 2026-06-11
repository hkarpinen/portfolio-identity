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
    Task<IReadOnlyList<string>> GenerateRecoveryCodesAsync(AppUser user, int count = 10, CancellationToken cancellationToken = default);
}
