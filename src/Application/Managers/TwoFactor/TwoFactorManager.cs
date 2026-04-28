using Application;
using Application.Contracts;
using Application.Services;
using Domain.Aggregates.User;
using Domain.Repositories;

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

    public async Task<Result<EnableTwoFactorResponse>> EnableTwoFactorAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(new UserId(userId));

        if (user is null)
            return Result<EnableTwoFactorResponse>.Failure("User not found.");

        await _userRepository.ResetAuthenticatorKeyAsync(user);
        var key = await _userRepository.GetAuthenticatorKeyAsync(user);

        if (string.IsNullOrEmpty(key))
            return Result<EnableTwoFactorResponse>.Failure("Failed to generate authenticator key.");

        var uri = GenerateAuthenticatorUri(user.Email!, key);
        return Result<EnableTwoFactorResponse>.Success(new EnableTwoFactorResponse(key, uri));
    }

    public async Task<Result<LoginResult>> VerifyTwoFactorAsync(TwoFactorVerifyRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(Email.From(request.Email));

        if (user is null)
            return Result<LoginResult>.Failure("User not found.");

        var isValid = await _userRepository.VerifyTwoFactorTokenAsync(user, request.Code);

        if (!isValid)
            return Result<LoginResult>.Failure("Invalid verification code.");

        if (!user.TwoFactorEnabled)
            await _userRepository.SetTwoFactorEnabledAsync(user, true);

        var tokenResult = _jwtTokenGenerator.GenerateToken(user);
        return Result<LoginResult>.Success(new LoginResult(false, tokenResult.Token, tokenResult.ExpiresAt));
    }

    private static string GenerateAuthenticatorUri(string email, string key)
    {
        const string issuer = "Identity";
        return $"otpauth://totp/{issuer}:{email}?secret={key}&issuer={issuer}&digits=6";
    }
}
