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
    private readonly ISessionManager _sessions;

    public IdentityController(
        IAuthManager authManager,
        ITwoFactorManager twoFactorManager,
        IProfileManager profileManager,
        IUserQuery query,
        ISessionManager sessions)
    {
        _authManager = authManager;
        _twoFactorManager = twoFactorManager;
        _profileManager = profileManager;
        _query = query;
        _sessions = sessions;
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
        await StartSessionAsync(loginResult.UserId);
        return Ok(new { requiresTwoFactor = false });
    }

    /// <summary>
    /// Trades the refresh cookie for a new access token, rotating the refresh cookie as it goes.
    /// Anonymous by design: it runs precisely when the access token has already expired.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        var presented = Request.Cookies[RefreshCookie];
        if (string.IsNullOrWhiteSpace(presented))
            return Problem(detail: "No session to refresh.", statusCode: StatusCodes.Status401Unauthorized);

        var session = await _sessions.RefreshAsync(presented, UserAgent, CallerIp, ct);
        if (session is null)
        {
            // Expired, signed out, or a token presented after it was already spent. The cookies go
            // either way — leaving a dead refresh cookie in place just retries this forever.
            ClearAuthCookies();
            return Problem(detail: "That session has ended. Sign in again.", statusCode: StatusCodes.Status401Unauthorized);
        }

        var token = await _authManager.IssueAccessTokenAsync(session.UserId, ct);
        if (token is null)
        {
            ClearAuthCookies();
            return Problem(detail: "That session has ended. Sign in again.", statusCode: StatusCodes.Status401Unauthorized);
        }

        SetAccessTokenCookie(token.Token, token.ExpiresAt.UtcDateTime);
        SetRefreshCookie(session.RefreshToken, session.RefreshExpiresAt);
        return NoContent();
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
        await StartSessionAsync(result.Value.UserId);
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

        ClearAuthCookies();
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

    [HttpGet("sessions")]
    [Authorize]
    public async Task<IActionResult> ListSessions(CancellationToken ct)
        => Ok(await _sessions.ListAsync(User.GetUserId(), Request.Cookies[RefreshCookie], ct));

    [HttpPost("sessions/{sessionId:guid}/revoke")]
    [Authorize]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> RevokeSession([FromRoute] Guid sessionId, CancellationToken ct)
        => await _sessions.RevokeAsync(User.GetUserId(), sessionId, ct) ? NoContent() : NotFound();

    [HttpPost("sessions/revoke-others")]
    [Authorize]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> RevokeOtherSessions(CancellationToken ct)
    {
        await _sessions.RevokeOthersAsync(User.GetUserId(), Request.Cookies[RefreshCookie], ct);
        return NoContent();
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("access_token");
        return NoContent();
    }

    private const string RefreshCookie = "refresh_token";

    private string? UserAgent => Request.Headers.UserAgent.ToString() is { Length: > 0 } ua ? ua : null;
    private string? CallerIp => HttpContext.Connection.RemoteIpAddress?.ToString();

    private async Task StartSessionAsync(Guid userId)
    {
        var session = await _sessions.StartAsync(userId, UserAgent, CallerIp, HttpContext.RequestAborted);
        SetRefreshCookie(session.RefreshToken, session.RefreshExpiresAt);
    }

    // Path is "/" rather than the refresh route, which is a deliberate trade. Scoping it would
    // keep the cookie away from the other services on this origin, but it would also keep it away
    // from the frontend's middleware — and a full page load is exactly when a lapsed session needs
    // renewing. HttpOnly, Secure and SameSite=Strict are what actually defend this cookie; a path
    // is not a security boundary in any browser.
    private void SetRefreshCookie(string token, DateTime expiresAtUtc) =>
        Response.Cookies.Append(RefreshCookie, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Expires = AsOffset(expiresAtUtc)
        });

    private void ClearAuthCookies()
    {
        Response.Cookies.Delete("access_token");
        Response.Cookies.Delete(RefreshCookie, new CookieOptions { Path = "/" });
    }

    private static DateTimeOffset AsOffset(DateTime utc) =>
        new(utc.Kind == DateTimeKind.Utc ? utc : DateTime.SpecifyKind(utc, DateTimeKind.Utc), TimeSpan.Zero);

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
