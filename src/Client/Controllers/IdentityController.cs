using Application.Commands;
using Application.Dtos;
using Application.Queries;
using Client.Extensions;
using Identity.Application.Managers.Auth;
using Identity.Application.Managers.Profile;
using Identity.Application.Managers.TwoFactor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Client.Controllers;

[ApiController]
[Route("api/identity")]
[EnableRateLimiting("standard")]
public sealed class IdentityController : ControllerBase
{
    private readonly IAuthManager _authManager;
    private readonly ITwoFactorManager _twoFactorManager;
    private readonly IProfileManager _profileManager;
    private readonly IUserQuery _query;

    public IdentityController(
        IAuthManager authManager,
        ITwoFactorManager twoFactorManager,
        IProfileManager profileManager,
        IUserQuery query)
    {
        _authManager = authManager;
        _twoFactorManager = twoFactorManager;
        _profileManager = profileManager;
        _query = query;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        var result = await _authManager.RegisterAsync(command);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created)
            : Problem(detail: result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginCommand command)
    {
        var result = await _authManager.LoginAsync(command);
        if (!result.IsSuccess)
            return Problem(detail: result.Error, statusCode: StatusCodes.Status400BadRequest);

        var loginResult = result.Value!;

        if (loginResult.RequiresTwoFactor)
            return Ok(new { requiresTwoFactor = true });

        SetAccessTokenCookie(loginResult.Token!, loginResult.ExpiresAt!.Value);
        return Ok(new { requiresTwoFactor = false });
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordCommand command)
    {
        await _authManager.ForgotPasswordAsync(command);
        return NoContent();
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResetPassword(ResetPasswordCommand command)
    {
        var result = await _authManager.ResetPasswordAsync(command);
        return result.IsSuccess
            ? NoContent()
            : Problem(detail: result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    [HttpPost("resend-confirmation")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResendConfirmationEmail(ResendConfirmationEmailCommand command)
    {
        await _authManager.ResendConfirmationEmailAsync(command);
        return NoContent();
    }

    [HttpPost("confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailCommand command)
    {
        var result = await _authManager.ConfirmEmailAsync(command);
        return result.IsSuccess
            ? Ok()
            : Problem(detail: result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    [HttpPost("2fa/enable")]
    [Authorize]
    public async Task<IActionResult> EnableTwoFactor()
    {
        var userId = User.GetUserId();
        var result = await _twoFactorManager.EnableTwoFactorAsync(userId);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(detail: result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    [HttpPost("2fa/verify")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> VerifyTwoFactor(VerifyTwoFactorCommand command)
    {
        var result = await _twoFactorManager.VerifyTwoFactorAsync(command);
        if (!result.IsSuccess)
            return Problem(detail: result.Error, statusCode: StatusCodes.Status400BadRequest);

        SetAccessTokenCookie(result.Value!.Token!, result.Value.ExpiresAt!.Value);
        return Ok();
    }

    /// <summary>Runs INSIDE a session, unlike the anonymous sign-in challenge.</summary>
    [HttpPost("2fa/confirm")]
    [Authorize]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ConfirmTwoFactor([FromBody] ConfirmTwoFactorCommand command)
    {
        var userId = User.GetUserId();
        var result = await _twoFactorManager.ConfirmTwoFactorAsync(userId, command);
        return result.IsSuccess
            ? NoContent()
            : Problem(detail: result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    [HttpPost("2fa/disable")]
    [Authorize]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> DisableTwoFactor([FromBody] DisableTwoFactorCommand command)
    {
        var userId = User.GetUserId();
        var result = await _twoFactorManager.DisableTwoFactorAsync(userId, command);
        return result.IsSuccess
            ? NoContent()
            : Problem(detail: result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>Reads the count only. Minting is the POST below — never this.</summary>
    [HttpGet("2fa/recovery-codes")]
    [Authorize]
    public async Task<IActionResult> GetRecoveryCodeStatus()
    {
        var userId = User.GetUserId();
        var result = await _twoFactorManager.GetRecoveryCodeStatusAsync(userId);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(detail: result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>Invalidates every previous code, and returns the new set only once.</summary>
    [HttpPost("2fa/recovery-codes/regenerate")]
    [Authorize]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> RegenerateRecoveryCodes()
    {
        var userId = User.GetUserId();
        var result = await _twoFactorManager.GenerateRecoveryCodesAsync(userId);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(detail: result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.GetUserId();
        var result = await _query.GetProfileAsync(userId);
        return result is not null
            ? Ok(result)
            : Problem(detail: "User not found.", statusCode: StatusCodes.Status404NotFound);
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile(UpdateProfileCommand command)
    {
        var userId = User.GetUserId();
        var result = await _profileManager.UpdateProfileAsync(userId, command);
        return result.IsSuccess
            ? NoContent()
            : Problem(detail: result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    [HttpPost("me/avatar")]
    [Authorize]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return Problem(detail: "File is required.", statusCode: StatusCodes.Status400BadRequest);

        var userId = User.GetUserId();

        await using var stream = file.OpenReadStream();
        var command = new UploadAvatarCommand(stream, file.ContentType, file.Length);
        var result = await _profileManager.UploadAvatarAsync(userId, command);

        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(detail: result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>In-session change. `reset-password` is the emailed-token flow for
    /// someone who cannot sign in, and is not a substitute.</summary>
    [HttpPut("password")]
    [Authorize]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand command)
    {
        var userId = User.GetUserId();
        var result = await _profileManager.ChangePasswordAsync(userId, command);
        return result.IsSuccess
            ? NoContent()
            : Problem(detail: result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    [HttpPut("email")]
    [Authorize]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailCommand command)
    {
        var userId = User.GetUserId();
        var result = await _profileManager.ChangeEmailAsync(userId, command);
        return result.IsSuccess
            ? NoContent()
            : Problem(detail: result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    [HttpDelete("me")]
    [Authorize]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountCommand command)
    {
        var userId = User.GetUserId();
        var result = await _profileManager.DeleteAccountAsync(userId, command);
        if (!result.IsSuccess)
            return Problem(detail: result.Error, statusCode: StatusCodes.Status400BadRequest);

        Response.Cookies.Delete("access_token");
        return NoContent();
    }

    [HttpGet("connections")]
    [Authorize]
    public async Task<IActionResult> GetConnections()
    {
        var userId = User.GetUserId();
        var result = await _profileManager.GetConnectionsAsync(userId);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(detail: result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    [HttpDelete("connections/{provider}")]
    [Authorize]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> DisconnectOAuth([FromRoute] string provider)
    {
        var userId = User.GetUserId();
        var result = await _profileManager.DisconnectOAuthAsync(userId, new DisconnectOAuthCommand(provider));
        return result.IsSuccess
            ? NoContent()
            : Problem(detail: result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    // Auth is one stateless cookie with no session store, so these answer 501 rather
    // than fabricating rows or a "revoke" that reports success and does nothing.
    private const string SessionsNotImplemented =
        "Session management is not available: this service does not maintain a session store.";

    [HttpGet("sessions")]
    [Authorize]
    public IActionResult ListSessions()
        => Problem(detail: SessionsNotImplemented, statusCode: StatusCodes.Status501NotImplemented);

    [HttpPost("sessions/{sessionId:guid}/revoke")]
    [Authorize]
    [EnableRateLimiting("write")]
    public IActionResult RevokeSession([FromRoute] Guid sessionId)
        => Problem(detail: SessionsNotImplemented, statusCode: StatusCodes.Status501NotImplemented);

    [HttpPost("sessions/revoke-others")]
    [Authorize]
    [EnableRateLimiting("write")]
    public IActionResult RevokeOtherSessions()
        => Problem(detail: SessionsNotImplemented, statusCode: StatusCodes.Status501NotImplemented);

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("access_token");
        return NoContent();
    }

    private void SetAccessTokenCookie(string token, DateTime expiresAtUtc)
    {
        var expires = expiresAtUtc.Kind == DateTimeKind.Utc
            ? new DateTimeOffset(expiresAtUtc, TimeSpan.Zero)
            : new DateTimeOffset(DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc), TimeSpan.Zero);

        Response.Cookies.Append("access_token", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = expires
        });
    }
}
