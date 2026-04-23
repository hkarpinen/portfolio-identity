namespace Application.Contracts;

public sealed record UserProfileResponse(
    Guid Id,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    string Role,
    bool IsEmailConfirmed,
    bool TwoFactorEnabled,
    DateTime CreatedAt);
