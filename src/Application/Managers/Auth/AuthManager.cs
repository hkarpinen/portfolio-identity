using Application;
using Application.Contracts;
using Application.Services;
using Domain.Aggregates.User;
using Domain.Repositories;
using Domain.Services;

namespace Identity.Application.Managers.Auth;

internal sealed class AuthManager : IAuthManager
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordAuthenticationEngine _passwordEngine;
    private readonly IEmailGateway _emailGateway;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthManager(
        IUserRepository userRepository,
        IPasswordAuthenticationEngine passwordEngine,
        IEmailGateway emailGateway,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _passwordEngine = passwordEngine;
        _emailGateway = emailGateway;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<Result> RegisterAsync(RegisterRequest request)
    {
        var email = Email.From(request.Email);
        var user = AppUser.Create(email, request.DisplayName);

        var (succeeded, error) = await _userRepository.CreateWithPasswordAsync(user, request.Password);

        if (!succeeded)
            return Result.Failure(error!);

        var token = await _userRepository.GenerateEmailConfirmationTokenAsync(user);
        await _emailGateway.SendConfirmationEmailAsync(user.Email!, user.Id.ToString(), token, user.DisplayName);

        return Result.Success();
    }

    public async Task<Result<LoginResult>> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(Email.From(request.Email));

        if (user is null)
            return Result<LoginResult>.Failure("Invalid email or password.");

        if (!user.EmailConfirmed)
            return Result<LoginResult>.Failure("Email not confirmed.");

        var check = await _passwordEngine.CheckPasswordAsync(user, request.Password);

        if (check.IsLockedOut)
            return Result<LoginResult>.Failure("Account is locked out.");

        if (!check.Succeeded)
            return Result<LoginResult>.Failure("Invalid email or password.");

        if (user.TwoFactorEnabled)
            return Result<LoginResult>.Success(new LoginResult(RequiresTwoFactor: true, Token: null));

        var tokenResult = _jwtTokenGenerator.GenerateToken(user);
        return Result<LoginResult>.Success(new LoginResult(false, tokenResult.Token, tokenResult.ExpiresAt));
    }

    public async Task<Result> ConfirmEmailAsync(ConfirmEmailRequest request)
    {
        var user = await _userRepository.GetByIdAsync(new UserId(Guid.Parse(request.UserId)));

        if (user is null)
            return Result.Failure("User not found.");

        if (user.EmailConfirmed)
            return Result.Success();

        var (succeeded, error) = await _userRepository.ConfirmEmailAsync(user, request.Token);
        return succeeded ? Result.Success() : Result.Failure(error!);
    }
}
