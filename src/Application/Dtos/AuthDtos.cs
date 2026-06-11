namespace Application.Dtos;

public sealed record LoginDto(bool RequiresTwoFactor, string? Token, DateTimeOffset? ExpiresAt = null);
public sealed record UploadAvatarDto(string AvatarUrl);
public sealed record TwoFactorSetupDto(string SharedKey, string AuthenticatorUri);

public sealed record AdminUserListDto(IReadOnlyList<AdminUserDto> Items, int TotalCount);
public sealed record UserProfileDto(
    Guid Id,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    string? Handle,
    string? Bio,
    string? Location,
    string? Pronouns,
    string Role,
    bool IsEmailConfirmed,
    bool TwoFactorEnabled,
    DateTime? TwoFactorEnabledAt,
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
