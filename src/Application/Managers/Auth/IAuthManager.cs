using Application;
using Application.Contracts;

namespace Identity.Application.Managers.Auth;

public interface IAuthManager
{
    Task<Result> RegisterAsync(RegisterRequest request);
    Task<Result<LoginResult>> LoginAsync(LoginRequest request);
    Task<Result> ConfirmEmailAsync(ConfirmEmailRequest request);
}
