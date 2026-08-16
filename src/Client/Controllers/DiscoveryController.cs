using Application.Ports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Client.Controllers;

/// <summary>
/// The two documents every other service needs in order to verify a token without holding a
/// secret. Anonymous by necessity — a service fetching the key set has no token yet, and the
/// contents are public keys.
/// </summary>
[ApiController]
[AllowAnonymous]
public sealed class DiscoveryController : ControllerBase
{
    private readonly IJwksProvider _jwks;

    public DiscoveryController(IJwksProvider jwks)
    {
        _jwks = jwks;
    }

    /// <summary>
    /// Minimal OIDC discovery. Consumers set <c>options.Authority</c> and the JWT bearer handler
    /// reads this to find <c>jwks_uri</c>, which is what lets them configure a URL instead of
    /// key material. This is not a full OIDC provider and does not pretend to be one.
    /// </summary>
    [HttpGet("/.well-known/openid-configuration")]
    public IActionResult Configuration()
    {
        var origin = $"{Request.Scheme}://{Request.Host}";
        return Ok(new
        {
            issuer = _jwks.Issuer,
            jwks_uri = $"{origin}/.well-known/jwks.json",
            id_token_signing_alg_values_supported = new[] { "ES256" }
        });
    }

    [HttpGet("/.well-known/jwks.json")]
    public ContentResult Keys() => Content(_jwks.JwkSetJson, "application/json");
}
