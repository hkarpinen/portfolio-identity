namespace Infrastructure.Messaging.Events;

public sealed record DemoUserCreatedEvent(
    Guid Id,
    DateTime OccurredAt,
    Guid UserId,
    string Email,
    string DisplayName,
    DateTime DemoExpiresAt);
