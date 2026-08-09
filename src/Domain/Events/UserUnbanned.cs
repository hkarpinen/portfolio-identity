namespace Domain.Events;

/// <summary>
/// An admin let a locked-out account back in. Mirrors <see cref="UserBanned"/>
/// — the Admin design promises "You can let them back in at any time. Nothing
/// is deleted", and a ban that could only ever be applied made that untrue.
/// </summary>
public sealed record UserUnbanned(
    Guid Id,
    DateTime OccurredAt,
    Guid UserId,
    DateTime UnbannedAt) : IDomainEvent;
