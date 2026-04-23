namespace Infrastructure.Messaging.Events;

/// <summary>
/// Wire message published to RabbitMQ when a user is banned.
/// Shape is kept flat to match the JSON-serialised UserBanned domain event.
/// </summary>
public sealed record UserBannedEvent(
    Guid Id,
    DateTime OccurredAt,
    Guid UserId,
    DateTime BannedAt);
