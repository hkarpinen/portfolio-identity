using Application;
using Application.Commands;
using Application.Dtos;

namespace Identity.Application.Managers.TwoFactor;

public interface ITwoFactorManager
{
    Task<Result<TwoFactorSetupDto>> EnableTwoFactorAsync(Guid userId);
    Task<Result<LoginDto>> VerifyTwoFactorAsync(VerifyTwoFactorCommand command);
}
