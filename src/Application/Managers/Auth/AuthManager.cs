using Application;
using Application.Commands;
using Application.Dtos;
using Application.Ports;
using Application.Repositories;
using Domain.Aggregates.User;

namespace Identity.Application.Managers.Auth;

internal sealed class AuthManager : IAuthManager
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordAuthenticationEngine _passwordEngine;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRecaptchaService _recaptcha;

    public AuthManager(
        IUserRepository userRepository,
        IPasswordAuthenticationEngine passwordEngine,
        IJwtTokenGenerator jwtTokenGenerator,
        IRecaptchaService recaptcha)
    {
        _userRepository = userRepository;
        _passwordEngine = passwordEngine;
        _jwtTokenGenerator = jwtTokenGenerator;
        _recaptcha = recaptcha;
    }

    public async Task<Result> RegisterAsync(RegisterCommand command)
    {
        if (!await _recaptcha.VerifyAsync(command.CaptchaToken, "register"))
            return Result.Failure("CAPTCHA verification failed. Please try again.");

        var email = Email.From(command.Email);
        var user = AppUser.Create(email, command.DisplayName);

        var (succeeded, error) = await _userRepository.CreateWithPasswordAsync(user, command.Password);

        if (!succeeded)
            return Result.Failure(error!);

        var token = await _userRepository.GenerateEmailConfirmationTokenAsync(user);
        await _userRepository.QueueConfirmationEmailAsync(user, token);

        return Result.Success();
    }

    public async Task<Result<LoginDto>> LoginAsync(LoginCommand command)
    {
        var user = await _userRepository.GetByEmailAsync(Email.From(command.Email));

        if (user is null)
            return Result<LoginDto>.Failure("Invalid email or password.");

        if (!user.EmailConfirmed)
            return Result<LoginDto>.Failure("Email not confirmed.");

        var check = await _passwordEngine.CheckPasswordAsync(user, command.Password);

        if (check.IsLockedOut)
            return Result<LoginDto>.Failure("Account is locked out.");

        if (!check.Succeeded)
            return Result<LoginDto>.Failure("Invalid email or password.");

        if (user.TwoFactorEnabled)
            return Result<LoginDto>.Success(new LoginDto(RequiresTwoFactor: true, Token: null));

        var tokenResult = _jwtTokenGenerator.GenerateToken(user);
        return Result<LoginDto>.Success(new LoginDto(false, tokenResult.Token, tokenResult.ExpiresAt));
    }

    public async Task ForgotPasswordAsync(ForgotPasswordCommand command)
    {
        var user = await _userRepository.GetByEmailAsync(Email.From(command.Email));
        if (user is null || !user.EmailConfirmed) return;

        var token = await _userRepository.GeneratePasswordResetTokenAsync(user);
        await _userRepository.QueuePasswordResetEmailAsync(user, token);
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordCommand command)
    {
        if (!Guid.TryParse(command.UserId, out var id))
            return Result.Failure("Invalid reset link.");

        var user = await _userRepository.GetByIdAsync(new UserId(id));
        if (user is null)
            return Result.Failure("Invalid reset link.");

        var (succeeded, error) = await _userRepository.ResetPasswordAsync(user, command.Token, command.NewPassword);
        return succeeded ? Result.Success() : Result.Failure(error!);
    }

    public async Task ResendConfirmationEmailAsync(ResendConfirmationEmailCommand command)
    {
        var user = await _userRepository.GetByEmailAsync(Email.From(command.Email));
        if (user is null || user.EmailConfirmed) return;

        var token = await _userRepository.GenerateEmailConfirmationTokenAsync(user);
        await _userRepository.QueueConfirmationEmailAsync(user, token);
    }

    public async Task<Result> ConfirmEmailAsync(ConfirmEmailCommand command)
    {
        var user = await _userRepository.GetByIdAsync(new UserId(Guid.Parse(command.UserId)));

        if (user is null)
            return Result.Failure("User not found.");

        if (user.EmailConfirmed)
            return Result.Success();

        var (succeeded, error) = await _userRepository.ConfirmEmailAsync(user, command.Token);
        return succeeded ? Result.Success() : Result.Failure(error!);
    }
}
