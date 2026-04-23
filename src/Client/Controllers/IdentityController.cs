using Application;
using Application.Contracts;
using Application.Managers;
using Client.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Client.Controllers;

[ApiController]
[Route("api/identity")]
[EnableRateLimiting("standard")]
public sealed class IdentityController : ControllerBase
{
    private readonly IIdentityManager _manager;

    public IdentityController(IIdentityManager manager)
    {
        _manager = manager;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var result = await _manager.RegisterAsync(request);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created)
            : BadRequest(new { error = result.Error });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await _manager.LoginAsync(request);
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
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailRequest request)
    {
        var result = await _manager.ConfirmEmailAsync(request);
        return result.IsSuccess
            ? Ok()
            : BadRequest(new { error = result.Error });
    }

    [HttpPost("2fa/enable")]
    [Authorize]
    public async Task<IActionResult> EnableTwoFactor()
    {
        var userId = User.GetUserId();
        var result = await _manager.EnableTwoFactorAsync(userId);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error });
    }

    [HttpPost("2fa/verify")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> VerifyTwoFactor(TwoFactorVerifyRequest request)
    {
        var result = await _manager.VerifyTwoFactorAsync(request);
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
        var result = await _manager.GetProfileAsync(userId);
        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(new { error = result.Error });
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request)
    {
        var userId = User.GetUserId();
        var result = await _manager.UpdateProfileAsync(userId, request);
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
        var request = new UploadAvatarRequest(stream, file.ContentType, file.Length);
        var result = await _manager.UploadAvatarAsync(userId, request);

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
