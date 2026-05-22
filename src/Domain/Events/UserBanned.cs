namespace Domain.Events;

public sealed record UserBanned(
    Guid Id,
    DateTime OccurredAt,
    Guid UserId,
    DateTime BannedAt) : IDomainEvent;
