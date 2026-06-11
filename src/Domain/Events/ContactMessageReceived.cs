namespace Domain.Events;

public sealed record ContactMessageReceived(
    Guid Id,
    DateTime OccurredAt,
    string SenderName,
    string SenderEmail,
    string Subject,
    string Message) : IDomainEvent;
