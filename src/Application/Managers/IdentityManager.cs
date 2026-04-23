using Application.Contracts;
using Application.Services;
using Domain.Aggregates.User;
using Domain.Repositories;
using Domain.Services;
using Utilities;

namespace Application.Managers;

internal sealed class IdentityManager : IIdentityManager
{
    private const long MaxAvatarBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedAvatarContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/webp",
        "image/gif"
    };

    private readonly IUserRepository _userRepository;
    private readonly IPasswordAuthenticationEngine _passwordEngine;
    private readonly IEmailGateway _emailGateway;
    private readonly IClock _clock;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IFileStorage _fileStorage;

    public IdentityManager(
        IUserRepository userRepository,
        IPasswordAuthenticationEngine passwordEngine,
        IEmailGateway emailGateway,
        IClock clock,
        IJwtTokenGenerator jwtTokenGenerator,
        IFileStorage fileStorage)
    {
        _userRepository = userRepository;
        _passwordEngine = passwordEngine;
        _emailGateway = emailGateway;
        _clock = clock;
        _jwtTokenGenerator = jwtTokenGenerator;
        _fileStorage = fileStorage;
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

    public async Task<Result<UserProfileResponse>> GetProfileAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(new UserId(userId));

        if (user is null)
            return Result<UserProfileResponse>.Failure("User not found.");

        var response = new UserProfileResponse(
            user.Id,
            user.Email!,
            user.DisplayName,
            user.AvatarUrl,
            user.Role.ToString(),
            user.EmailConfirmed,
            user.TwoFactorEnabled,
            user.CreatedAt);

        return Result<UserProfileResponse>.Success(response);
    }

    public async Task<Result> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
    {
        var user = await _userRepository.GetByIdAsync(new UserId(userId));

        if (user is null)
            return Result.Failure("User not found.");

        user.UpdateProfile(request.DisplayName, request.AvatarUrl);

        await _userRepository.SaveAsync(user);
        return Result.Success();
    }

    public async Task<Result<UploadAvatarResponse>> UploadAvatarAsync(Guid userId, UploadAvatarRequest request)
    {
        if (request.Length <= 0)
            return Result<UploadAvatarResponse>.Failure("File is empty.");

        if (request.Length > MaxAvatarBytes)
            return Result<UploadAvatarResponse>.Failure("File exceeds the 5 MB limit.");

        if (!AllowedAvatarContentTypes.Contains(request.ContentType))
            return Result<UploadAvatarResponse>.Failure("Unsupported image type. Use PNG, JPEG, WebP, or GIF.");

        var user = await _userRepository.GetByIdAsync(new UserId(userId));
        if (user is null)
            return Result<UploadAvatarResponse>.Failure("User not found.");

        var extension = request.ContentType.ToLowerInvariant() switch
        {
            "image/png" => "png",
            "image/jpeg" => "jpg",
            "image/webp" => "webp",
            "image/gif" => "gif",
            _ => "bin"
        };

        var key = $"identity/users/{userId}/avatar.{extension}";
        var avatarUrl = await _fileStorage.SaveAsync(key, request.Content, request.ContentType);

        user.ChangeAvatar(avatarUrl);
        await _userRepository.SaveAsync(user);

        return Result<UploadAvatarResponse>.Success(new UploadAvatarResponse(avatarUrl));
    }

    public async Task<Result> BanAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(new UserId(userId));

        if (user is null)
            return Result.Failure("User not found.");

        user.Ban();

        await _userRepository.SaveAsync(user);
        return Result.Success();
    }

    private static string GenerateAuthenticatorUri(string email, string key)
    {
        const string issuer = "Identity";
        return $"otpauth://totp/{issuer}:{email}?secret={key}&issuer={issuer}&digits=6";
    }
}
