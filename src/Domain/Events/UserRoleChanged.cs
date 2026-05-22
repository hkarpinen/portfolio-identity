namespace Domain.Events;

public sealed record UserRoleChanged(
    Guid Id,
    DateTime OccurredAt,
    Guid UserId,
    string NewRole) : IDomainEvent;
