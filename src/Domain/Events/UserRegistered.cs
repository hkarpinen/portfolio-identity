using Domain.Aggregates.User;

namespace Domain.Events;

public sealed record UserRegistered(
    Guid Id,
    DateTime OccurredAt,
    UserId UserId,
    string Email,
    string DisplayName) : IDomainEvent;
