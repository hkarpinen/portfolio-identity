using Application.Queries;
using Client.Extensions;
using Identity.Application.Managers.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Client.Controllers;

[ApiController]
[Route("api/identity/admin")]
[Authorize(Policy = "AdminOnly")]
[EnableRateLimiting("standard")]
public sealed class AdminController : ControllerBase
{
    private readonly IAdminManager _adminManager;
    private readonly IUserQuery _query;

    public AdminController(IAdminManager adminManager, IUserQuery query)
    {
        _adminManager = adminManager;
        _query = query;
    }

    [HttpGet("users")]
    public async Task<IActionResult> ListUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var (items, _) = await _query.ListUsersAsync(page, pageSize);
        return Ok(items);
    }

    [HttpPost("users/{id:guid}/ban")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> BanUser([FromRoute] Guid id)
    {
        var result = await _adminManager.BanAsync(id);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }

    [HttpPost("users/{id:guid}/role")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> ChangeRole([FromRoute] Guid id, [FromBody] ChangeRoleBody body)
    {
        var result = await _adminManager.ChangeRoleAsync(id, body.Role);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }
}

public sealed record ChangeRoleBody(string Role);
