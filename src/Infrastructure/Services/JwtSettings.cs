namespace Infrastructure.Services;

public sealed class JwtSettings
{
    /// <summary>Base64-encoded PKCS#8 PEM of the EC P-256 private key. Identity alone holds it.</summary>
    public string PrivateKeyPem { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 15;
}
