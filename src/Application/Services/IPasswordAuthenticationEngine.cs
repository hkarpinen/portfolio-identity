using Domain.Aggregates.User;

namespace Application.Services;

public sealed record PasswordCheckResult(bool Succeeded, bool IsLockedOut);

public interface IPasswordAuthenticationEngine
{
    Task<PasswordCheckResult> CheckPasswordAsync(AppUser user, string password, CancellationToken cancellationToken = default);
}
