namespace Application.Contracts;

public sealed record TwoFactorVerifyRequest(string Code, string Email);
