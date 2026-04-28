using Application;
using Application.Contracts;

namespace Identity.Application.Managers.TwoFactor;

public interface ITwoFactorManager
{
    Task<Result<EnableTwoFactorResponse>> EnableTwoFactorAsync(Guid userId);
    Task<Result<LoginResult>> VerifyTwoFactorAsync(TwoFactorVerifyRequest request);
}
