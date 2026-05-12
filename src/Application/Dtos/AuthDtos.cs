namespace Application.Dtos;

public sealed record LoginDto(bool RequiresTwoFactor, string? Token, DateTimeOffset? ExpiresAt = null);
public sealed record UploadAvatarDto(string AvatarUrl);
public sealed record TwoFactorSetupDto(string SharedKey, string AuthenticatorUri);

public sealed record UserProfileDto(
    Guid Id,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    string Role,
    bool IsEmailConfirmed,
    bool TwoFactorEnabled,
    DateTime CreatedAt);

public sealed record AdminUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    string Role,
    bool IsBanned,
    bool IsEmailConfirmed,
    DateTime CreatedAt);
