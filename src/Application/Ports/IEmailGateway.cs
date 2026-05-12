namespace Application.Ports;

public interface IEmailGateway
{
    Task SendConfirmationEmailAsync(string email, string userId, string token, string displayName, CancellationToken cancellationToken = default);
    Task SendNotificationEmailAsync(string email, string subject, string body, CancellationToken cancellationToken = default);
}
