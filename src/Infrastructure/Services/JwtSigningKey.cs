using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.Ports;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Services;

/// <summary>
/// The one signing key in the system. The private half never leaves identity; everyone else
/// verifies against the public half served at the JWKS endpoint.
/// </summary>
internal sealed class JwtSigningKey : IJwksProvider, IDisposable
{
    private readonly ECDsa _ecdsa;

    public SigningCredentials Credentials { get; }
    public string JwkSetJson { get; }
    public string Issuer { get; }

    public JwtSigningKey(IOptions<JwtSettings> settings)
    {
        var configured = settings.Value.PrivateKeyPem;
        if (string.IsNullOrWhiteSpace(configured))
            throw new InvalidOperationException(
                "Jwt:PrivateKeyPem must be configured — a PKCS#8 EC P-256 private key. " +
                "Generate one with: openssl ecparam -genkey -name prime256v1 -noout " +
                "| openssl pkcs8 -topk8 -nocrypt | base64");

        _ecdsa = ECDsa.Create();
        Import(_ecdsa, configured);

        Issuer = settings.Value.Issuer;

        // Export without the private parameters: `d` must never reach the JWK set.
        var parameters = _ecdsa.ExportParameters(includePrivateParameters: false);
        var x = Base64UrlEncoder.Encode(parameters.Q.X);
        var y = Base64UrlEncoder.Encode(parameters.Q.Y);

        // A thumbprint kid rather than a random one, so restarting identity does not invalidate
        // the key sets consumers have already cached.
        var kid = Base64UrlEncoder.Encode(
            new JsonWebKey { Kty = "EC", Crv = "P-256", X = x, Y = y }.ComputeJwkThumbprint());

        var signingKey = new ECDsaSecurityKey(_ecdsa) { KeyId = kid };
        Credentials = new SigningCredentials(signingKey, SecurityAlgorithms.EcdsaSha256);

        JwkSetJson = JsonSerializer.Serialize(new
        {
            keys = new[]
            {
                new { kty = "EC", crv = "P-256", use = "sig", alg = "ES256", kid, x, y }
            }
        });
    }

    /// <summary>
    /// Accepts the key however it arrives: a PEM pasted directly, the base64 of a PEM file, or the
    /// base64 of raw PKCS#8 DER. A PEM's newlines rarely survive a round trip through an env var
    /// or a secret store intact, so which form a deployment ends up with is not worth guessing at.
    /// </summary>
    private static void Import(ECDsa ecdsa, string configured)
    {
        var value = configured.Trim();

        if (value.Contains("-----BEGIN", StringComparison.Ordinal))
        {
            ecdsa.ImportFromPem(value);
            return;
        }

        byte[] decoded;
        try
        {
            decoded = Convert.FromBase64String(value);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException(
                "Jwt:PrivateKeyPem is neither a PEM nor valid base64. Expected the output of: " +
                "openssl ecparam -genkey -name prime256v1 -noout | openssl pkcs8 -topk8 -nocrypt | base64", ex);
        }

        var text = Encoding.UTF8.GetString(decoded);
        if (text.Contains("-----BEGIN", StringComparison.Ordinal))
        {
            ecdsa.ImportFromPem(text);
            return;
        }

        ecdsa.ImportPkcs8PrivateKey(decoded, out _);
    }

    public void Dispose() => _ecdsa.Dispose();
}
