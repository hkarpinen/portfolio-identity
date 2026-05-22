using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Application.Ports;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

internal sealed class RecaptchaService : IRecaptchaService
{
    private const string VerifyUrl = "https://www.google.com/recaptcha/api/siteverify";

    private readonly HttpClient _http;
    private readonly RecaptchaOptions _options;

    public RecaptchaService(HttpClient http, IOptions<RecaptchaOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<bool> VerifyAsync(string token, string action, CancellationToken cancellationToken = default)
    {
        // Skip verification in dev when no secret key is configured.
        if (string.IsNullOrEmpty(_options.SecretKey))
            return true;

        if (string.IsNullOrEmpty(token))
            return false;

        var form = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("secret", _options.SecretKey),
            new KeyValuePair<string, string>("response", token),
        ]);

        var response = await _http.PostAsync(VerifyUrl, form, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<RecaptchaResponse>(cancellationToken: cancellationToken);

        return result is { Success: true }
            && result.Score >= _options.MinimumScore
            && string.Equals(result.Action, action, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class RecaptchaResponse
    {
        [JsonPropertyName("success")]    public bool   Success  { get; init; }
        [JsonPropertyName("score")]      public double Score    { get; init; }
        [JsonPropertyName("action")]     public string Action   { get; init; } = string.Empty;
        [JsonPropertyName("error-codes")] public string[]? ErrorCodes { get; init; }
    }
}
