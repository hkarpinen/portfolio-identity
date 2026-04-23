namespace Application.Contracts;

public sealed record UpdateProfileRequest(string DisplayName, string? AvatarUrl);
