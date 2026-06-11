namespace Application.Dtos;

public sealed record DemoStartRequestDto(string CaptchaToken);

public sealed record DemoStartDto(string Token, DateTime DemoExpiresAt);
