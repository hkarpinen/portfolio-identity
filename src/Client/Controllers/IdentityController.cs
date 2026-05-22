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
            : BadRequest(new { error = result.Error });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginCommand command)
    {
        var result = await _authManager.LoginAsync(command);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        var loginResult = result.Value!;

        if (loginResult.RequiresTwoFactor)
            return Ok(new { requiresTwoFactor = true });

        SetAccessTokenCookie(loginResult.Token!, loginResult.ExpiresAt!.Value);
        return Ok(new { requiresTwoFactor = false });
    }

    [HttpPost("confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailCommand command)
    {
        var result = await _authManager.ConfirmEmailAsync(command);
        return result.IsSuccess
            ? Ok()
            : BadRequest(new { error = result.Error });
    }

    [HttpPost("2fa/enable")]
    [Authorize]
    public async Task<IActionResult> EnableTwoFactor()
    {
        var userId = User.GetUserId();
        var result = await _twoFactorManager.EnableTwoFactorAsync(userId);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error });
    }

    [HttpPost("2fa/verify")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> VerifyTwoFactor(VerifyTwoFactorCommand command)
    {
        var result = await _twoFactorManager.VerifyTwoFactorAsync(command);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        SetAccessTokenCookie(result.Value!.Token!, result.Value.ExpiresAt!.Value);
        return Ok();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.GetUserId();
        var result = await _query.GetProfileAsync(userId);
        return result is not null
            ? Ok(result)
            : NotFound(new { error = "User not found." });
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile(UpdateProfileCommand command)
    {
        var userId = User.GetUserId();
        var result = await _profileManager.UpdateProfileAsync(userId, command);
        return result.IsSuccess
            ? NoContent()
            : BadRequest(new { error = result.Error });
    }

    [HttpPost("me/avatar")]
    [Authorize]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "File is required." });

        var userId = User.GetUserId();

        await using var stream = file.OpenReadStream();
        var command = new UploadAvatarCommand(stream, file.ContentType, file.Length);
        var result = await _profileManager.UploadAvatarAsync(userId, command);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error });
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("access_token");
        return NoContent();
    }

    private void SetAccessTokenCookie(string token, DateTimeOffset expiresAt)
    {
        Response.Cookies.Append("access_token", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = expiresAt
        });
    }
}
