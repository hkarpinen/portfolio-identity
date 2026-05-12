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
            await _userRepository.SetTwoFactorEnabledAsync(user, true);

        var tokenResult = _jwtTokenGenerator.GenerateToken(user);
        return Result<LoginDto>.Success(new LoginDto(false, tokenResult.Token, tokenResult.ExpiresAt));
    }

    private static string GenerateAuthenticatorUri(string email, string key)
    {
        const string issuer = "Identity";
        return $"otpauth://totp/{issuer}:{email}?secret={key}&issuer={issuer}&digits=6";
    }
}
