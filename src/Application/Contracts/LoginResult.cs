namespace Application.Contracts;

public sealed record LoginResult(bool RequiresTwoFactor, string? Token, DateTimeOffset? ExpiresAt = null);
