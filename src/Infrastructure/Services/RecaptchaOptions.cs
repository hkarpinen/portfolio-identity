namespace Infrastructure.Services;

public sealed class RecaptchaOptions
{
    public string SecretKey { get; set; } = string.Empty;
    public double MinimumScore { get; set; } = 0.5;
}
