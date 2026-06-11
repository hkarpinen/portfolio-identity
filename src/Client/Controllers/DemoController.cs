using Application.Dtos;
using Identity.Application.Managers.Demo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Client.Controllers;

[ApiController]
[Route("api/identity/demo")]
[EnableRateLimiting("standard")]
public sealed class DemoController : ControllerBase
{
    private readonly IDemoManager _demoManager;

    public DemoController(IDemoManager demoManager)
    {
        _demoManager = demoManager;
    }

    [HttpPost("start")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Start([FromBody] DemoStartRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _demoManager.StartDemoAsync(request.CaptchaToken, cancellationToken);
        if (!result.IsSuccess)
            return Problem(detail: result.Error, statusCode: StatusCodes.Status400BadRequest);

        var dto = result.Value!;
        var expiresAt = new DateTimeOffset(dto.DemoExpiresAt, TimeSpan.Zero);

        Response.Cookies.Append("access_token", dto.Token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = expiresAt
        });

        return Ok(new { demoExpiresAt = dto.DemoExpiresAt });
    }
}
