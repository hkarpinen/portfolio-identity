namespace Application.Contracts;

public sealed record AdminUserResponse(
    Guid Id,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    string Role,
    bool IsBanned,
    bool IsEmailConfirmed,
    DateTime CreatedAt);
