using System.Web;
using Domain.Services;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

internal sealed class SmtpEmailGateway : IEmailGateway
{
    private readonly ILogger<SmtpEmailGateway> _logger;

    public SmtpEmailGateway(ILogger<SmtpEmailGateway> logger)
    {
        _logger = logger;
    }

    public Task SendConfirmationEmailAsync(string email, string userId, string token, string displayName, CancellationToken cancellationToken = default)
    {
        var confirmUrl = $"http://localhost/confirm-email?userId={userId}&token={HttpUtility.UrlEncode(token)}";
        _logger.LogInformation(
            "Confirm email for {DisplayName} ({Email}): {ConfirmUrl}",
            displayName, email, confirmUrl);
        return Task.CompletedTask;
    }

    public Task SendNotificationEmailAsync(string email, string subject, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Sending notification email to {Email} with subject '{Subject}': {Body}",
            email, subject, body);
        return Task.CompletedTask;
    }
}
