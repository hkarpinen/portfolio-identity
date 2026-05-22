using Application;
using Application.Commands;
using Application.Dtos;

namespace Identity.Application.Managers.Auth;

public interface IAuthManager
{
    Task<Result> RegisterAsync(RegisterCommand command);
    Task<Result<LoginDto>> LoginAsync(LoginCommand command);
    Task<Result> ConfirmEmailAsync(ConfirmEmailCommand command);
    Task ResendConfirmationEmailAsync(ResendConfirmationEmailCommand command);
    Task ForgotPasswordAsync(ForgotPasswordCommand command);
    Task<Result> ResetPasswordAsync(ResetPasswordCommand command);
}
