using Domain.Aggregates.User;

namespace Domain.Events;

public sealed record DemoUserCreated(
    Guid Id,
    DateTime OccurredAt,
    UserId UserId,
    string Email,
    string DisplayName,
    DateTime DemoExpiresAt) : IDomainEvent;
