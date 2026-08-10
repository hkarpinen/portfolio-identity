using Application;
using Application.Commands;
using Application.Dtos;
using Application.Ports;

namespace Identity.Application.Managers.Auth;

public interface IAuthManager
{
    Task<Result> RegisterAsync(RegisterCommand command);
    Task<Result<LoginDto>> LoginAsync(LoginCommand command);
    Task<Result> ConfirmEmailAsync(ConfirmEmailCommand command);
    Task ResendConfirmationEmailAsync(ResendConfirmationEmailCommand command);
    Task ForgotPasswordAsync(ForgotPasswordCommand command);
    Task<Result> ResetPasswordAsync(ResetPasswordCommand command);

    /// <summary>A fresh access token for an already-established session. Null when the account has
    /// gone away or been banned since — a live refresh token must not outlive the account.</summary>
    Task<TokenResult?> IssueAccessTokenAsync(Guid userId, CancellationToken ct = default);
}
