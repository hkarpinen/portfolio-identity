using Domain.Events;

namespace Domain.Aggregates.Contact;

public sealed class ContactMessage
{
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    public Guid Id { get; private set; }
    public string SenderName { get; private set; } = string.Empty;
    public string SenderEmail { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public DateTime ReceivedAt { get; private set; }

    private ContactMessage() { }

    public static ContactMessage Submit(string senderName, string senderEmail, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(senderName)) throw new ArgumentException("Sender name required", nameof(senderName));
        if (string.IsNullOrWhiteSpace(senderEmail)) throw new ArgumentException("Sender email required", nameof(senderEmail));
        if (string.IsNullOrWhiteSpace(subject)) throw new ArgumentException("Subject required", nameof(subject));
        if (string.IsNullOrWhiteSpace(body)) throw new ArgumentException("Message body required", nameof(body));

        var msg = new ContactMessage
        {
            Id = Guid.NewGuid(),
            SenderName = senderName.Trim(),
            SenderEmail = senderEmail.Trim(),
            Subject = subject.Trim(),
            Body = body.Trim(),
            ReceivedAt = DateTime.UtcNow
        };
        msg._domainEvents.Add(new ContactMessageReceived(
            Guid.NewGuid(),
            msg.ReceivedAt,
            msg.SenderName,
            msg.SenderEmail,
            msg.Subject,
            msg.Body));
        return msg;
    }
}
