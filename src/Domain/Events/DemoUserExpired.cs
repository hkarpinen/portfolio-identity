namespace Domain.Events;

public sealed record DemoUserExpired(
    Guid Id,
    DateTime OccurredAt,
    Guid UserId) : IDomainEvent;
