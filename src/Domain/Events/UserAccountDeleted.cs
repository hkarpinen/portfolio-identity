namespace Domain.Events;

public sealed record UserAccountDeleted(
    Guid Id,
    DateTime OccurredAt,
    Guid UserId,
    DateTime DeletedAt) : IDomainEvent;
