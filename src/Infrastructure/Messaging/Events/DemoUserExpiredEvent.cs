namespace Infrastructure.Messaging.Events;

public sealed record DemoUserExpiredEvent(
    Guid Id,
    DateTime OccurredAt,
    Guid UserId);
