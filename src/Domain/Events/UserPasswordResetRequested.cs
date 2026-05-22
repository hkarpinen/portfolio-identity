namespace Domain.Events;

public sealed record UserPasswordResetRequested(
    Guid Id,
    DateTime OccurredAt,
    Guid UserId,
    string Email,
    string DisplayName,
    string ResetToken) : IDomainEvent;
