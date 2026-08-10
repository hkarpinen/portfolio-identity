using Application;
using Application.Commands;
using Application.Dtos;
using Application.Ports;
using Application.Repositories;
using Domain.Aggregates.User;

namespace Identity.Application.Managers.TwoFactor;

internal sealed class TwoFactorManager : ITwoFactorManager
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public TwoFactorManager(IUserRepository userRepository, IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<Result<TwoFactorSetupDto>> EnableTwoFactorAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(new UserId(userId));

        if (user is null)
            return Result<TwoFactorSetupDto>.Failure("User not found.");

        await _userRepository.ResetAuthenticatorKeyAsync(user);
        var key = await _userRepository.GetAuthenticatorKeyAsync(user);

        if (string.IsNullOrEmpty(key))
            return Result<TwoFactorSetupDto>.Failure("Failed to generate authenticator key.");

        var uri = GenerateAuthenticatorUri(user.Email!, key);
        return Result<TwoFactorSetupDto>.Success(new TwoFactorSetupDto(key, uri));
    }

    public async Task<Result<LoginDto>> VerifyTwoFactorAsync(VerifyTwoFactorCommand command)
    {
        var user = await _userRepository.GetByEmailAsync(Email.From(command.Email));

        if (user is null)
            return Result<LoginDto>.Failure("User not found.");

        var isValid = await _userRepository.VerifyTwoFactorTokenAsync(user, command.Code);

        if (!isValid)
            return Result<LoginDto>.Failure("Invalid verification code.");

        if (!user.TwoFactorEnabled)
        {
            // Set together so the flag and its timestamp cannot diverge.
            user.MarkTwoFactorEnabled();
            await _userRepository.SaveAsync(user);
        }

        var tokenResult = _jwtTokenGenerator.GenerateToken(user);
        return Result<LoginDto>.Success(new LoginDto(false, tokenResult.Token, tokenResult.ExpiresAt.UtcDateTime, user.Id));
    }

    /// <summary>
    /// Finishes setup for a signed-in user. The sign-in challenge is a different
    /// operation: anonymous, keyed by email, and it mints a cookie — wrong here.
    /// </summary>
    public async Task<Result> ConfirmTwoFactorAsync(Guid userId, ConfirmTwoFactorCommand command)
    {
        var user = await _userRepository.GetByIdAsync(new UserId(userId));
        if (user is null) return Result.Failure("User not found.");

        if (!await _userRepository.VerifyTwoFactorTokenAsync(user, command.Code))
            return Result.Failure("That code didn't match. Try the next one your app shows.");

        if (!user.TwoFactorEnabled)
        {
            // Set together so the flag and its timestamp cannot diverge.
            user.MarkTwoFactorEnabled();
            await _userRepository.SaveAsync(user);
        }

        return Result.Success();
    }

    public async Task<Result> DisableTwoFactorAsync(Guid userId, DisableTwoFactorCommand command)
    {
        var user = await _userRepository.GetByIdAsync(new UserId(userId));
        if (user is null) return Result.Failure("User not found.");

        if (!user.TwoFactorEnabled)
            return Result.Failure("Two-factor authentication is not currently enabled.");

        if (!await _userRepository.VerifyTwoFactorTokenAsync(user, command.CurrentCode))
            return Result.Failure("Invalid verification code.");

        // Cleared together so the flag and its timestamp cannot diverge.
        user.MarkTwoFactorDisabled();
        await _userRepository.SaveAsync(user);
        return Result.Success();
    }

    public async Task<Result<TwoFactorRecoveryStatusDto>> GetRecoveryCodeStatusAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(new UserId(userId));
        if (user is null)
            return Result<TwoFactorRecoveryStatusDto>.Failure("User not found.");

        if (!user.TwoFactorEnabled)
            return Result<TwoFactorRecoveryStatusDto>.Success(new TwoFactorRecoveryStatusDto(0));

        var remaining = await _userRepository.CountRecoveryCodesAsync(user);
        return Result<TwoFactorRecoveryStatusDto>.Success(new TwoFactorRecoveryStatusDto(remaining));
    }

    public async Task<Result<TwoFactorRecoveryCodesDto>> GenerateRecoveryCodesAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(new UserId(userId));
        if (user is null)
            return Result<TwoFactorRecoveryCodesDto>.Failure("User not found.");

        if (!user.TwoFactorEnabled)
            return Result<TwoFactorRecoveryCodesDto>.Failure("Two-factor authentication must be enabled before generating recovery codes.");

        var codes = await _userRepository.GenerateRecoveryCodesAsync(user);
        return Result<TwoFactorRecoveryCodesDto>.Success(new TwoFactorRecoveryCodesDto(codes));
    }

    private static string GenerateAuthenticatorUri(string email, string key)
    {
        const string issuer = "Identity";
        return $"otpauth://totp/{issuer}:{email}?secret={key}&issuer={issuer}&digits=6";
    }
}
