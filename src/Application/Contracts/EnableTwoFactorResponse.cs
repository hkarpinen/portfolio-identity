namespace Application.Contracts;

public sealed record EnableTwoFactorResponse(string SharedKey, string AuthenticatorUri);
