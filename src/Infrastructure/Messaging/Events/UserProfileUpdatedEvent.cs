namespace Infrastructure.Messaging.Events;

/// <summary>
/// Wire message published to RabbitMQ when a user updates their profile.
/// Shape is kept flat to match the JSON-serialised UserProfileUpdated domain event.
/// </summary>
public sealed record UserProfileUpdatedEvent(
    Guid Id,
    DateTime OccurredAt,
    Guid UserId,
    string DisplayName,
    string? AvatarUrl);
