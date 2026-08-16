using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Domain.Aggregates.User;
using Infrastructure.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Tests;

public class JwtTokenGeneratorTests
{
    private static JwtSettings CreateSettings(int expirationMinutes = 60)
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return new JwtSettings
        {
            PrivateKeyPem = Convert.ToBase64String(
                Encoding.UTF8.GetBytes(ecdsa.ExportPkcs8PrivateKeyPem())),
            Issuer = "test-issuer",
            Audience = "test-audience",
            ExpirationMinutes = expirationMinutes
        };
    }

    private static JwtTokenGenerator CreateGenerator(int expirationMinutes = 60)
    {
        var settings = CreateSettings(expirationMinutes);
        return new JwtTokenGenerator(Options.Create(settings), new JwtSigningKey(Options.Create(settings)));
    }

    private static AppUser CreateUser(string email = "user@example.com", string displayName = "Test User", UserRole role = UserRole.Member)
    {
        return AppUser.Create(Email.From(email), displayName, role);
    }

    [Fact]
    public void GenerateToken_ShouldReturnNonEmptyString()
    {
        var generator = CreateGenerator();
        var user = CreateUser();

        var result = generator.GenerateToken(user);

        Assert.NotNull(result);
        Assert.NotNull(result.Token);
        Assert.NotEmpty(result.Token);
        Assert.True(result.ExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public void GenerateToken_ShouldContain_SubClaim_WithUserId()
    {
        var generator = CreateGenerator();
        var user = CreateUser();
        var handler = new JwtSecurityTokenHandler();

        var result = generator.GenerateToken(user);
        var jwt = handler.ReadJwtToken(result.Token);

        var sub = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
        Assert.Equal(user.Id.ToString(), sub);
    }

    /// <summary>
    /// The token says who you are, not what you may do. Profile data on it was a denormalised copy
    /// that went stale for a token lifetime; the role vocabulary on it was what leaked identity's
    /// enum into forum, which allow-listed the names and 403'd every demo write when `Demo` was
    /// added. Both are regressions worth failing a build over.
    /// </summary>
    [Fact]
    public void GenerateToken_ShouldCarryNothingButIdentity()
    {
        var generator = CreateGenerator();
        var user = CreateUser("alice@example.com", "Alice Wonder");
        var handler = new JwtSecurityTokenHandler();

        var jwt = handler.ReadJwtToken(generator.GenerateToken(user).Token);
        var types = jwt.Claims.Select(c => c.Type).ToHashSet();

        Assert.DoesNotContain(JwtRegisteredClaimNames.Email, types);
        Assert.DoesNotContain("displayName", types);
        Assert.DoesNotContain("avatarUrl", types);
        Assert.DoesNotContain("role", types);
    }

    [Fact]
    public void GenerateToken_ShouldCarryAdminClaim_ForAdministrators()
    {
        var generator = CreateGenerator();
        var handler = new JwtSecurityTokenHandler();

        var jwt = handler.ReadJwtToken(generator.GenerateToken(CreateUser(role: UserRole.Admin)).Token);

        Assert.Equal("true", jwt.Claims.FirstOrDefault(c => c.Type == "admin")?.Value);
    }

    /// <summary>
    /// Absent rather than "false", so a consumer that forgets to compare the value still gets the
    /// safe answer from a bare presence check.
    /// </summary>
    [Theory]
    [InlineData(UserRole.Member)]
    [InlineData(UserRole.Demo)]
    public void GenerateToken_ShouldOmitAdminClaim_ForEveryoneElse(UserRole role)
    {
        var generator = CreateGenerator();
        var handler = new JwtSecurityTokenHandler();

        var jwt = handler.ReadJwtToken(generator.GenerateToken(CreateUser(role: role)).Token);

        Assert.DoesNotContain("admin", jwt.Claims.Select(c => c.Type));
    }

    /// <summary>
    /// The point of the whole scheme: a consumer holding nothing but the published key set can
    /// verify the signature. Nothing secret crosses a service boundary.
    /// </summary>
    [Fact]
    public void GenerateToken_ShouldBeValidatable_WithThePublishedPublicKey()
    {
        var settings = CreateSettings();
        var signingKey = new JwtSigningKey(Options.Create(settings));
        var generator = new JwtTokenGenerator(Options.Create(settings), signingKey);
        var user = CreateUser();
        var handler = new JwtSecurityTokenHandler();

        var result = generator.GenerateToken(user);

        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = settings.Issuer,
            ValidateAudience = true,
            ValidAudience = settings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = new JsonWebKeySet(signingKey.JwkSetJson).GetSigningKeys(),
            ValidateLifetime = true
        };

        var principal = handler.ValidateToken(result.Token, validationParams, out _);
        Assert.NotNull(principal);
    }

    /// <summary>
    /// A JWK set is served to anyone who asks. Exporting with private parameters would put `d` —
    /// the private scalar — on a public endpoint, and hand every reader the ability to sign.
    /// </summary>
    [Fact]
    public void JwkSet_ShouldCarryNoPrivateKeyMaterial()
    {
        var signingKey = new JwtSigningKey(Options.Create(CreateSettings()));

        using var document = JsonDocument.Parse(signingKey.JwkSetJson);
        var jwk = document.RootElement.GetProperty("keys").EnumerateArray().Single();

        Assert.False(jwk.TryGetProperty("d", out _));
        Assert.Equal("EC", jwk.GetProperty("kty").GetString());
        Assert.Equal("ES256", jwk.GetProperty("alg").GetString());
        Assert.False(string.IsNullOrEmpty(jwk.GetProperty("kid").GetString()));
    }

    /// <summary>Consumers cache key sets by `kid`; a restart must not invalidate them.</summary>
    [Fact]
    public void JwkSet_ShouldDeriveAStableKeyId_FromTheKeyItself()
    {
        var settings = CreateSettings();

        var first = new JwtSigningKey(Options.Create(settings)).JwkSetJson;
        var second = new JwtSigningKey(Options.Create(settings)).JwkSetJson;

        Assert.Equal(first, second);
    }

    [Fact]
    public void SigningKey_ShouldRefuseToStart_WithoutAConfiguredKey()
    {
        var settings = new JwtSettings { Issuer = "test-issuer", Audience = "test-audience" };

        Assert.Throws<InvalidOperationException>(() => new JwtSigningKey(Options.Create(settings)));
    }

    [Fact]
    public void GenerateToken_ShouldHaveCorrectIssuerAndAudience()
    {
        var generator = CreateGenerator();
        var user = CreateUser();
        var handler = new JwtSecurityTokenHandler();

        var result = generator.GenerateToken(user);
        var jwt = handler.ReadJwtToken(result.Token);

        Assert.Equal("test-issuer", jwt.Issuer);
        Assert.Contains("test-audience", jwt.Audiences);
    }
}
