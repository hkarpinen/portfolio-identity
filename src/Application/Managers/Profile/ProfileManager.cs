using Application;
using Application.Contracts;
using Application.Services;
using Domain.Aggregates.User;
using Domain.Repositories;

namespace Identity.Application.Managers.Profile;

internal sealed class ProfileManager : IProfileManager
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
    private readonly IFileStorage _fileStorage;

    public ProfileManager(IUserRepository userRepository, IFileStorage fileStorage)
    {
        _userRepository = userRepository;
        _fileStorage = fileStorage;
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
}
