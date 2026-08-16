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
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new("displayName", user.DisplayName),
            new("role", user.Role.ToString())
        };

        if (!string.IsNullOrEmpty(user.AvatarUrl))
            claims.Add(new Claim("avatarUrl", user.AvatarUrl));

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
