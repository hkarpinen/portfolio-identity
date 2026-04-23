namespace Application.Contracts;

public sealed record RegisterRequest(string Email, string Password, string DisplayName);
