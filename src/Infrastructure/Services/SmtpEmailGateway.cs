using System.Web;
using Application.Ports;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Infrastructure.Services;

internal sealed class SmtpEmailGateway : IEmailGateway
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailGateway> _logger;

    public SmtpEmailGateway(IOptions<EmailOptions> options, ILogger<SmtpEmailGateway> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendConfirmationEmailAsync(string email, string userId, string token, string displayName, CancellationToken cancellationToken = default)
    {
        var confirmUrl = $"{_options.BaseUrl}/confirm-email?userId={userId}&token={HttpUtility.UrlEncode(token)}";

        var body = $"""
            Hi {displayName},

            Please confirm your email address by clicking the link below:

            {confirmUrl}

            If you did not create an account, you can ignore this email.
            """;

        await SendAsync(email, "Confirm your email", body, cancellationToken);
    }

    public async Task SendNotificationEmailAsync(string email, string subject, string body, CancellationToken cancellationToken = default)
    {
        await SendAsync(email, subject, body, cancellationToken);
    }

    private async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        try
        {
            var hasCredentials = !string.IsNullOrWhiteSpace(_options.Username);
            var socketOptions = hasCredentials ? SecureSocketOptions.Auto : SecureSocketOptions.None;
            await client.ConnectAsync(_options.Host, _options.Port, socketOptions, cancellationToken);
            if (hasCredentials)
                await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To} with subject '{Subject}'", to, subject);
            throw;
        }
    }
}
