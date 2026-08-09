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
public sealed record ChangeEmailCommand(string NewEmail, string CurrentPassword);

/// <summary>
/// Change the password from inside a signed-in session. The current password is
/// the re-authentication, exactly as `ChangeEmailCommand` uses it — a stolen
/// session must not be able to lock the owner out of their own account.
/// </summary>
public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword);

/// <summary>
/// Finish enabling 2FA: the six digits from the app, proving the shared key
/// actually reached it. Distinct from <see cref="VerifyTwoFactorCommand"/>,
/// which is the SIGN-IN challenge and is keyed by email because there is no
/// session yet. This one runs inside a session and needs no email.
/// </summary>
public sealed record ConfirmTwoFactorCommand(string Code);
public sealed record DisableTwoFactorCommand(string CurrentCode);
public sealed record DeleteAccountCommand(string ConfirmationDisplayName);
public sealed record DisconnectOAuthCommand(string Provider);
