using Domain.Aggregates.User;

namespace Domain.Events;

public sealed record UserProfileUpdated(
    Guid Id,
    DateTime OccurredAt,
    UserId UserId,
    string DisplayName,
    string? AvatarUrl) : IDomainEvent;
