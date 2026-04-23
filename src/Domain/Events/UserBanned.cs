using Domain.Aggregates.User;

namespace Domain.Events;

public sealed record UserBanned(
    Guid Id,
    DateTime OccurredAt,
    UserId UserId,
    DateTime BannedAt) : IDomainEvent;
