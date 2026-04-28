using Application.Managers;
using Application.Queries;
using Client.Extensions;
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
    private readonly IIdentityManager _manager;
    private readonly IIdentityQuery _query;

    public AdminController(IIdentityManager manager, IIdentityQuery query)
    {
        _manager = manager;
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
        var result = await _manager.BanAsync(id);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }

    [HttpPost("users/{id:guid}/role")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> ChangeRole([FromRoute] Guid id, [FromBody] ChangeRoleBody body)
    {
        var result = await _manager.ChangeRoleAsync(id, body.Role);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }
}

public sealed record ChangeRoleBody(string Role);
