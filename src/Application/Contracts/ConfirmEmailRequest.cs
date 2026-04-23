namespace Application.Contracts;

public sealed record ConfirmEmailRequest(string UserId, string Token);
