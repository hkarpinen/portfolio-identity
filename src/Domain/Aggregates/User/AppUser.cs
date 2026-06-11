using Domain.Events;
using Microsoft.AspNetCore.Identity;

namespace Domain.Aggregates.User;

public class AppUser : IdentityUser<Guid>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public string DisplayName { get; private set; } = string.Empty;
    public string? AvatarUrl { get; private set; }
    public string? Handle { get; private set; }
    public string? Bio { get; private set; }
    public string? Location { get; private set; }
    public string? Pronouns { get; private set; }
    public UserRole Role { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? TwoFactorEnabledAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public bool IsDemo { get; private set; }
    public DateTime? DemoExpiresAt { get; private set; }
    public DateTime? DemoExpiredAt { get; private set; }

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    private AppUser() { }

    public static AppUser Create(Email email, string displayName, UserRole role = UserRole.Member)
    {
        var userId = UserId.New();
        var now = DateTime.UtcNow;

        var user = new AppUser
        {
            Id = userId.Value,
            UserName = email.Value,
            Email = email.Value,
            DisplayName = displayName,
            Role = role,
            CreatedAt = now
        };

        user._domainEvents.Add(new UserRegistered(
            Guid.NewGuid(),
            now,
            userId.Value,
            email.Value,
            displayName));

        return user;
    }

    public void UpdateProfile(
        string displayName,
        string? avatarUrl,
        string? handle = null,
        string? bio = null,
        string? location = null,
        string? pronouns = null)
    {
        DisplayName = displayName;
        AvatarUrl = avatarUrl;
        Handle = handle;
        Bio = bio;
        Location = location;
        Pronouns = pronouns;

        _domainEvents.Add(new UserProfileUpdated(
            Guid.NewGuid(),
            DateTime.UtcNow,
            Id,
            displayName,
            avatarUrl));
    }

    /// <summary>
    /// Enables 2FA and stamps the enabled-at timestamp in a single mutation so the flag and
    /// timestamp cannot diverge. Both are persisted together by the next SaveAsync.
    /// </summary>
    public void MarkTwoFactorEnabled()
    {
        TwoFactorEnabled = true;
        TwoFactorEnabledAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Disables 2FA and clears the enabled-at timestamp in a single mutation.
    /// </summary>
    public void MarkTwoFactorDisabled()
    {
        TwoFactorEnabled = false;
        TwoFactorEnabledAt = null;
    }

    /// <summary>
    /// Soft-deletes the account: stamps DeletedAt, anonymises display fields, and locks out lifelong.
    /// The row is preserved so foreign-key references from forum content keep resolving to "[deleted]".
    /// </summary>
    public void SoftDelete()
    {
        var now = DateTime.UtcNow;
        DeletedAt = now;
        DisplayName = "[deleted]";
        Handle = null;
        Bio = null;
        Location = null;
        Pronouns = null;
        AvatarUrl = null;
        LockoutEnabled = true;
        LockoutEnd = DateTimeOffset.MaxValue;
        var anonEmail = $"deleted-{Id:N}@deleted.local";
        UserName = anonEmail;
        Email = anonEmail;
        NormalizedEmail = anonEmail.ToUpperInvariant();
        NormalizedUserName = anonEmail.ToUpperInvariant();
        EmailConfirmed = false;

        _domainEvents.Add(new UserAccountDeleted(
            Guid.NewGuid(),
            now,
            Id,
            now));
    }

    public void ChangeAvatar(string? avatarUrl)
    {
        AvatarUrl = avatarUrl;

        _domainEvents.Add(new UserProfileUpdated(
            Guid.NewGuid(),
            DateTime.UtcNow,
            Id,
            DisplayName,
            avatarUrl));
    }

    /// <summary>
    /// Marks the (already-changed) email address as unverified and requests a fresh confirmation.
    /// Forcing re-verification on email change is a domain rule, so the aggregate owns both the
    /// EmailConfirmed reset and the UserEmailConfirmationRequested event. The caller supplies the
    /// confirmation token because token generation is an Identity I/O concern handled by the repo.
    /// </summary>
    public void RequestEmailReverification(string confirmationToken)
    {
        EmailConfirmed = false;

        _domainEvents.Add(new UserEmailConfirmationRequested(
            Guid.NewGuid(),
            DateTime.UtcNow,
            Id,
            Email!,
            DisplayName,
            confirmationToken));
    }

    public void Ban()
    {
        var now = DateTime.UtcNow;
        LockoutEnabled = true;
        LockoutEnd = DateTimeOffset.MaxValue;

        _domainEvents.Add(new UserBanned(
            Guid.NewGuid(),
            now,
            Id,
            now));
    }

    public void ChangeRole(UserRole newRole)
    {
        Role = newRole;

        _domainEvents.Add(new UserRoleChanged(
            Guid.NewGuid(),
            DateTime.UtcNow,
            Id,
            newRole.ToString()));
    }

    public static AppUser CreateDemo(string displayName)
    {
        var userId = UserId.New();
        var now = DateTime.UtcNow;
        var expiresAt = now.AddHours(2);
        var email = $"demo-{userId.Value:N}@demo.internal";

        var user = new AppUser
        {
            Id = userId.Value,
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName,
            Role = UserRole.Demo,
            CreatedAt = now,
            IsDemo = true,
            DemoExpiresAt = expiresAt
        };

        user._domainEvents.Add(new DemoUserCreated(
            Guid.NewGuid(),
            now,
            userId.Value,
            email,
            displayName,
            expiresAt));

        return user;
    }

    public void ExpireDemo()
    {
        DemoExpiredAt = DateTime.UtcNow;

        _domainEvents.Add(new DemoUserExpired(
            Guid.NewGuid(),
            DemoExpiredAt.Value,
            Id));
    }
}
