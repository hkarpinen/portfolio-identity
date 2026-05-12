namespace Application.Commands;

public sealed record LoginCommand(string Email, string Password);
public sealed record RegisterCommand(string Email, string Password, string DisplayName);
public sealed record ConfirmEmailCommand(string UserId, string Token);
public sealed record VerifyTwoFactorCommand(string Code, string Email);
public sealed record UpdateProfileCommand(string DisplayName, string? AvatarUrl);
public sealed record UploadAvatarCommand(Stream Content, string ContentType, long Length);
