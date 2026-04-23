namespace Infrastructure.Messaging.Events;

/// <summary>
/// Wire message published to RabbitMQ when a user registers.
/// Shape is kept flat to match the JSON-serialised UserRegistered domain event.
/// </summary>
public sealed record UserRegisteredEvent(
    Guid Id,
    DateTime OccurredAt,
    Guid UserId,
    string Email,
    string DisplayName);
