using System.Security.Claims;

namespace Client.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? principal.FindFirstValue("sub")
                  ?? throw new InvalidOperationException("Missing user identifier claim.");
        return Guid.Parse(raw);
    }

}
