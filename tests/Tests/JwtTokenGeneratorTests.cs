using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Domain.Aggregates.User;
using Infrastructure.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Tests;

public class JwtTokenGeneratorTests
{
    private static JwtTokenGenerator CreateGenerator(int expirationMinutes = 60)
    {
        var settings = new JwtSettings
        {
            Secret = "super-secret-key-that-is-long-enough-for-hmac256",
            Issuer = "test-issuer",
            Audience = "test-audience",
            ExpirationMinutes = expirationMinutes
        };
        return new JwtTokenGenerator(Options.Create(settings));
    }

    private static AppUser CreateUser(string email = "user@example.com", string displayName = "Test User", UserRole role = UserRole.Member)
    {
        return AppUser.Create(Email.From(email), displayName, role);
    }

    [Fact]
    public void GenerateToken_ShouldReturnNonEmptyString()
    {
        // Arrange
        var generator = CreateGenerator();
        var user = CreateUser();

        // Act
        var result = generator.GenerateToken(user);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Token);
        Assert.NotEmpty(result.Token);
        Assert.True(result.ExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public void GenerateToken_ShouldContain_SubClaim_WithUserId()
    {
        // Arrange
        var generator = CreateGenerator();
        var user = CreateUser();
        var handler = new JwtSecurityTokenHandler();

        // Act
        var result = generator.GenerateToken(user);
        var jwt = handler.ReadJwtToken(result.Token);

        // Assert
        var sub = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
        Assert.Equal(user.Id.ToString(), sub);
    }

    [Fact]
    public void GenerateToken_ShouldContain_EmailClaim()
    {
        // Arrange
        var generator = CreateGenerator();
        var user = CreateUser("alice@example.com");
        var handler = new JwtSecurityTokenHandler();

        // Act
        var result = generator.GenerateToken(user);
        var jwt = handler.ReadJwtToken(result.Token);

        // Assert
        var email = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value;
        Assert.Equal("alice@example.com", email);
    }

    [Fact]
    public void GenerateToken_ShouldContain_DisplayNameClaim()
    {
        // Arrange
        var generator = CreateGenerator();
        var user = CreateUser(displayName: "Alice Wonder");
        var handler = new JwtSecurityTokenHandler();

        // Act
        var result = generator.GenerateToken(user);
        var jwt = handler.ReadJwtToken(result.Token);

        // Assert
        var displayName = jwt.Claims.FirstOrDefault(c => c.Type == "displayName")?.Value;
        Assert.Equal("Alice Wonder", displayName);
    }

    [Fact]
    public void GenerateToken_ShouldContain_RoleClaim()
    {
        // Arrange
        var generator = CreateGenerator();
        var user = CreateUser(role: UserRole.Admin);
        var handler = new JwtSecurityTokenHandler();

        // Act
        var result = generator.GenerateToken(user);
        var jwt = handler.ReadJwtToken(result.Token);

        // Assert
        var role = jwt.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
        Assert.Equal("Admin", role);
    }

    [Fact]
    public void GenerateToken_ShouldBeValidatable_WithSameKey()
    {
        // Arrange
        var settings = new JwtSettings
        {
            Secret = "super-secret-key-that-is-long-enough-for-hmac256",
            Issuer = "test-issuer",
            Audience = "test-audience",
            ExpirationMinutes = 60
        };
        var generator = new JwtTokenGenerator(Options.Create(settings));
        var user = CreateUser();
        var handler = new JwtSecurityTokenHandler();

        // Act
        var result = generator.GenerateToken(user);

        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = settings.Issuer,
            ValidateAudience = true,
            ValidAudience = settings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret)),
            ValidateLifetime = true
        };

        // Assert - should not throw
        var principal = handler.ValidateToken(result.Token, validationParams, out _);
        Assert.NotNull(principal);
    }

    [Fact]
    public void GenerateToken_ShouldHaveCorrectIssuerAndAudience()
    {
        // Arrange
        var generator = CreateGenerator();
        var user = CreateUser();
        var handler = new JwtSecurityTokenHandler();

        // Act
        var result = generator.GenerateToken(user);
        var jwt = handler.ReadJwtToken(result.Token);

        // Assert
        Assert.Equal("test-issuer", jwt.Issuer);
        Assert.Contains("test-audience", jwt.Audiences);
    }
}
