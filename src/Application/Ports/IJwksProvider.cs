namespace Application.Ports;

/// <summary>
/// Publishes the public half of the signing key so every other service can verify a token without
/// holding anything secret. Returns serialised JSON rather than a key type: the shape is a wire
/// format (RFC 7517), and nothing above Infrastructure should have an opinion about key objects.
/// </summary>
public interface IJwksProvider
{
    /// <summary>A JWK Set — <c>{"keys":[…]}</c> — carrying the public key only.</summary>
    string JwkSetJson { get; }

    /// <summary>The value tokens carry as <c>iss</c>, echoed in the discovery document.</summary>
    string Issuer { get; }
}
