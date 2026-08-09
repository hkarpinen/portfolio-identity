namespace Application.Dtos;

public sealed record LoginDto(bool RequiresTwoFactor, string? Token, DateTime? ExpiresAt = null);
public sealed record UploadAvatarDto(string AvatarUrl);
public sealed record TwoFactorSetupDto(string SharedKey, string AuthenticatorUri);

/// <summary>A freshly minted set. Returned once, at the moment of minting.</summary>
public sealed record TwoFactorRecoveryCodesDto(IReadOnlyList<string> Codes);

/// <summary>How many recovery codes are left — read on every visit to the Security screen.
/// Separate from the codes themselves, which are shown exactly once and never re-readable.</summary>
public sealed record TwoFactorRecoveryStatusDto(int Remaining);

public sealed record OAuthConnectionDto(string Provider, bool Connected, string? Handle);
public sealed record ConnectionsResponseDto(OAuthConnectionDto Github, OAuthConnectionDto Google);

public sealed record AdminUserListDto(IReadOnlyList<AdminUserDto> Items, int TotalCount);

public sealed record ContactMessageDto(
    string Name,
    string Email,
    string Subject,
    string Message,
    string CaptchaToken);

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
    DateTime CreatedAt,
    /// <summary>When the lockout was applied. Null unless banned.</summary>
    DateTime? BannedAt,
    /// <summary>The admin's own words. Null when none was given.</summary>
    string? BanReason);
