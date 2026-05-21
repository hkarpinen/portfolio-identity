using Application;
using Application.Commands;
using Application.Dtos;
using Application.Ports;
using Application.Repositories;
using Domain.Aggregates.User;

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

    // Guards against content-type spoofing
    private static readonly Dictionary<string, byte[]> AvatarMagicBytes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = [0xFF, 0xD8, 0xFF],
        ["image/png"]  = [0x89, 0x50, 0x4E, 0x47],
        ["image/gif"]  = [0x47, 0x49, 0x46, 0x38],
        ["image/webp"] = [0x52, 0x49, 0x46, 0x46],
    };

    private static bool HasValidMagicBytes(Stream stream, string contentType)
    {
        if (!AvatarMagicBytes.TryGetValue(contentType, out var sig)) return false;
        Span<byte> header = stackalloc byte[8];
        var read = stream.Read(header);
        if (stream.CanSeek) stream.Position = 0;
        return header[..read].StartsWith(sig);
    }

    private readonly IUserRepository _userRepository;
    private readonly IFileStorage _fileStorage;

    public ProfileManager(IUserRepository userRepository, IFileStorage fileStorage)
    {
        _userRepository = userRepository;
        _fileStorage = fileStorage;
    }

    public async Task<Result> UpdateProfileAsync(Guid userId, UpdateProfileCommand command)
    {
        var user = await _userRepository.GetByIdAsync(new UserId(userId));

        if (user is null)
            return Result.Failure("User not found.");

        user.UpdateProfile(command.DisplayName, command.AvatarUrl);

        await _userRepository.SaveAsync(user);
        return Result.Success();
    }

    public async Task<Result<UploadAvatarDto>> UploadAvatarAsync(Guid userId, UploadAvatarCommand command)
    {
        if (command.Length <= 0)
            return Result<UploadAvatarDto>.Failure("File is empty.");

        if (command.Length > MaxAvatarBytes)
            return Result<UploadAvatarDto>.Failure("File exceeds the 5 MB limit.");

        if (!AllowedAvatarContentTypes.Contains(command.ContentType))
            return Result<UploadAvatarDto>.Failure("Unsupported image type. Use PNG, JPEG, WebP, or GIF.");

        if (!HasValidMagicBytes(command.Content, command.ContentType))
            return Result<UploadAvatarDto>.Failure("File content does not match the declared type.");

        var user = await _userRepository.GetByIdAsync(new UserId(userId));
        if (user is null)
            return Result<UploadAvatarDto>.Failure("User not found.");

        var extension = command.ContentType.ToLowerInvariant() switch
        {
            "image/png" => "png",
            "image/jpeg" => "jpg",
            "image/webp" => "webp",
            "image/gif" => "gif",
            _ => "bin"
        };

        var key = $"identity/users/{userId}/avatar.{extension}";
        var avatarUrl = await _fileStorage.SaveAsync(key, command.Content, command.ContentType);

        user.ChangeAvatar(avatarUrl);
        await _userRepository.SaveAsync(user);

        return Result<UploadAvatarDto>.Success(new UploadAvatarDto(avatarUrl));
    }
}
