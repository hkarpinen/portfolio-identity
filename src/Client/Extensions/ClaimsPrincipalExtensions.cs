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

    public static string GetRole(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.Role)
        ?? throw new InvalidOperationException("Missing role claim.");

    public static string GetEmail(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.Email)
        ?? throw new InvalidOperationException("Missing email claim.");
}
