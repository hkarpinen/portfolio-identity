namespace Application.Ports;

public interface IRecaptchaService
{
    /// <summary>
    /// Verifies a reCAPTCHA v3 token and returns true when the score meets the
    /// configured minimum. Returns true unconditionally when no secret key is
    /// configured (local development).
    /// </summary>
    Task<bool> VerifyAsync(string token, string action, CancellationToken cancellationToken = default);
}
