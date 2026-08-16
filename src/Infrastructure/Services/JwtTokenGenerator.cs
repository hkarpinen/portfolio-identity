using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Ports;
using Domain.Aggregates.User;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Services;

internal sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtSettings _settings;
    private readonly JwtSigningKey _signingKey;

    public JwtTokenGenerator(IOptions<JwtSettings> settings, JwtSigningKey signingKey)
    {
        _settings = settings.Value;
        _signingKey = signingKey;
    }

    public TokenResult GenerateToken(AppUser user, DateTimeOffset? overrideExpiry = null)
    {
        // The token says who you are, not what you may do. Anything a service needs in order to
        // authorise is that service's own — forum reads its community memberships, household its
        // memberships, and both keep an IsDemo projection already. Putting identity's role
        // vocabulary in here is what let it leak: adding `Demo` once 403'd every forum write,
        // because forum was allow-listing role names it does not own.
        //
        // `admin` is the exception, and only because "is this a platform administrator" is a fact
        // identity holds about its own account. It is a boolean, not an enum, so identity can add
        // roles forever without breaking a consumer.
        //
        // Email, display name and avatar are gone too: they were denormalised copies that went
        // stale for a token lifetime, and nothing read them off the token. The frontend asks
        // /api/identity/me.
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString())
        };

        if (user.Role == UserRole.Admin)
            claims.Add(new Claim("admin", "true"));

        var expiresAt = overrideExpiry ?? DateTimeOffset.UtcNow.AddMinutes(_settings.ExpirationMinutes);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: _signingKey.Credentials);

        return new TokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
