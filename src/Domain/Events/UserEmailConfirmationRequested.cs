namespace Domain.Events;

public sealed record UserEmailConfirmationRequested(
    Guid Id,
    DateTime OccurredAt,
    Guid UserId,
    string Email,
    string DisplayName,
    string ConfirmationToken) : IDomainEvent;
