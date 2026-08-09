using Application;
using Application.Commands;
using Application.Dtos;

namespace Identity.Application.Managers.TwoFactor;

public interface ITwoFactorManager
{
    Task<Result<TwoFactorSetupDto>> EnableTwoFactorAsync(Guid userId);
    Task<Result<LoginDto>> VerifyTwoFactorAsync(VerifyTwoFactorCommand command);

    /// <summary>
    /// Finish setup for the signed-in user. Turning 2FA on must not depend on
    /// the sign-in challenge, which is anonymous and keyed by email.
    /// </summary>
    Task<Result> ConfirmTwoFactorAsync(Guid userId, ConfirmTwoFactorCommand command);
    Task<Result> DisableTwoFactorAsync(Guid userId, DisableTwoFactorCommand command);
    /// <summary>How many codes are left. Read-only — mints nothing.</summary>
    Task<Result<TwoFactorRecoveryStatusDto>> GetRecoveryCodeStatusAsync(Guid userId);

    /// <summary>
    /// Mints a fresh set, invalidating every previous code. Destructive, so it
    /// is only ever reached by an explicit POST.
    /// </summary>
    Task<Result<TwoFactorRecoveryCodesDto>> GenerateRecoveryCodesAsync(Guid userId);
}
