using Domain.Aggregates.User;

namespace Domain.Events;

public sealed record DemoUserExpired(
    Guid Id,
    DateTime OccurredAt,
    UserId UserId) : IDomainEvent;
