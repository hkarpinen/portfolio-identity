using Application.Dtos;
using Identity.Application.Managers.Contact;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Client.Controllers;

[ApiController]
[Route("api/identity/contact")]
[AllowAnonymous]
[EnableRateLimiting("auth")]
public sealed class ContactController : ControllerBase
{
    private readonly IContactManager _contactManager;

    public ContactController(IContactManager contactManager)
    {
        _contactManager = contactManager;
    }

    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] ContactMessageDto message, CancellationToken cancellationToken)
    {
        var result = await _contactManager.SubmitAsync(message, cancellationToken);
        return result.IsSuccess
            ? Accepted()
            : Problem(detail: result.Error, statusCode: StatusCodes.Status400BadRequest);
    }
}
