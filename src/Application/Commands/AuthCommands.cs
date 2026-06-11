namespace Application.Commands;

public sealed record LoginCommand(string Email, string Password);
public sealed record RegisterCommand(string Email, string Password, string DisplayName, string CaptchaToken);
public sealed record ConfirmEmailCommand(string UserId, string Token);
public sealed record ResendConfirmationEmailCommand(string Email);
public sealed record ForgotPasswordCommand(string Email);
public sealed record ResetPasswordCommand(string UserId, string Token, string NewPassword);
public sealed record VerifyTwoFactorCommand(string Code, string Email);
public sealed record UpdateProfileCommand(
    string DisplayName,
    string? AvatarUrl,
    string? Handle = null,
    string? Bio = null,
    string? Location = null,
    string? Pronouns = null);
public sealed record UploadAvatarCommand(Stream Content, string ContentType, long Length);
