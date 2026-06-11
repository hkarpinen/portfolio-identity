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
