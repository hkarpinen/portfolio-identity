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

        user.UpdateProfile(
            command.DisplayName,
            command.AvatarUrl,
            command.Handle,
            command.Bio,
            command.Location,
            command.Pronouns);

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

    public async Task<Result> ChangeEmailAsync(Guid userId, ChangeEmailCommand command)
    {
        var user = await _userRepository.GetByIdAsync(new UserId(userId));
        if (user is null)
            return Result.Failure("User not found.");

        if (!await _userRepository.CheckPasswordAsync(user, command.CurrentPassword))
            return Result.Failure("Current password is incorrect.");

        Email parsedEmail;
        try { parsedEmail = Email.From(command.NewEmail); }
        catch (ArgumentException ex) { return Result.Failure(ex.Message); }

        if (string.Equals(user.Email, parsedEmail.Value, StringComparison.OrdinalIgnoreCase))
            return Result.Failure("New email matches the current email.");

        var existing = await _userRepository.GetByEmailAsync(parsedEmail);
        if (existing is not null && existing.Id != user.Id)
            return Result.Failure("That email address is already in use.");

        var (succeeded, error) = await _userRepository.ChangeEmailAsync(user, parsedEmail.Value);
        if (!succeeded)
            return Result.Failure(error!);

        // Changing the email forces re-verification: generate a confirmation token (Identity I/O),
        // then let the aggregate reset EmailConfirmed and raise UserEmailConfirmationRequested.
        // SaveAsync drains that event into the outbox so the confirmation email is dispatched.
        var confirmationToken = await _userRepository.GenerateEmailConfirmationTokenAsync(user);
        user.RequestEmailReverification(confirmationToken);
        await _userRepository.SaveAsync(user);

        return Result.Success();
    }

    public async Task<Result> DeleteAccountAsync(Guid userId, DeleteAccountCommand command)
    {
        var user = await _userRepository.GetByIdAsync(new UserId(userId));
        if (user is null) return Result.Failure("User not found.");

        if (!string.Equals(user.DisplayName, command.ConfirmationDisplayName, StringComparison.Ordinal))
            return Result.Failure("Confirmation does not match your display name.");

        user.SoftDelete();
        await _userRepository.SaveAsync(user);
        return Result.Success();
    }

    public async Task<Result<ConnectionsResponseDto>> GetConnectionsAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(new UserId(userId));
        if (user is null)
            return Result<ConnectionsResponseDto>.Failure("User not found.");

        var logins = await _userRepository.GetExternalLoginsAsync(user);
        OAuthConnectionDto githubDto = Build(logins, "GitHub");
        OAuthConnectionDto googleDto = Build(logins, "Google");
        return Result<ConnectionsResponseDto>.Success(new ConnectionsResponseDto(githubDto, googleDto));

        static OAuthConnectionDto Build(IReadOnlyList<(string Provider, string ProviderKey)> logins, string provider)
        {
            var match = logins.FirstOrDefault(l => string.Equals(l.Provider, provider, StringComparison.OrdinalIgnoreCase));
            return new OAuthConnectionDto(provider, match != default, match == default ? null : match.ProviderKey);
        }
    }

    public async Task<Result> DisconnectOAuthAsync(Guid userId, DisconnectOAuthCommand command)
    {
        var user = await _userRepository.GetByIdAsync(new UserId(userId));
        if (user is null) return Result.Failure("User not found.");

        // Refuse to disconnect the only login method the user has.
        var totalLogins = await _userRepository.CountPasswordsAndLoginsAsync(user);
        if (totalLogins <= 1)
            return Result.Failure("Cannot disconnect your only sign-in method. Set a password first.");

        var (succeeded, error) = await _userRepository.RemoveExternalLoginAsync(user, command.Provider);
        return succeeded ? Result.Success() : Result.Failure(error!);
    }
}
