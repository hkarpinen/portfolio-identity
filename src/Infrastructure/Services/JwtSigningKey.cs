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
                "Jwt:PrivateKeyPem must be configured — a base64-encoded PKCS#8 PEM. " +
                "Generate one with: openssl ecparam -genkey -name prime256v1 -noout " +
                "| openssl pkcs8 -topk8 -nocrypt | base64");

        // Base64 around the PEM, because a PEM's newlines do not survive an env var intact.
        _ecdsa = ECDsa.Create();
        _ecdsa.ImportFromPem(Encoding.UTF8.GetString(Convert.FromBase64String(configured)));

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

    public void Dispose() => _ecdsa.Dispose();
}
