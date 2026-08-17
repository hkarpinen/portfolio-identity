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
        // The token says who you are, not what you may do. Anything a service needs to authorise
        // is that service's own. `admin` is the exception: it is identity's fact about its own
        // account, and a boolean rather than an enum so new roles cannot break a consumer.
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
