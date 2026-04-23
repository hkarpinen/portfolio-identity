using Domain.Aggregates.User;

namespace Domain.Events;

public sealed record UserRoleChanged(
    Guid Id,
    DateTime OccurredAt,
    UserId UserId,
    UserRole NewRole) : IDomainEvent;
