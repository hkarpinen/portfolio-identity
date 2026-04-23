using Domain.Events;
using Microsoft.AspNetCore.Identity;

namespace Domain.Aggregates.User;

public class AppUser : IdentityUser<Guid>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public string DisplayName { get; private set; } = string.Empty;
    public string? AvatarUrl { get; private set; }
    public UserRole Role { get; private set; }
    public DateTime CreatedAt { get; private set; }

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
            userId,
            email.Value,
            displayName));

        return user;
    }

    public void UpdateProfile(string displayName, string? avatarUrl)
    {
        DisplayName = displayName;
        AvatarUrl = avatarUrl;

        _domainEvents.Add(new UserProfileUpdated(
            Guid.NewGuid(),
            DateTime.UtcNow,
            new UserId(Id),
            displayName,
            avatarUrl));
    }

    public void ChangeAvatar(string? avatarUrl)
    {
        AvatarUrl = avatarUrl;

        _domainEvents.Add(new UserProfileUpdated(
            Guid.NewGuid(),
            DateTime.UtcNow,
            new UserId(Id),
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
            new UserId(Id),
            now));
    }

    public void ChangeRole(UserRole newRole)
    {
        Role = newRole;

        _domainEvents.Add(new UserRoleChanged(
            Guid.NewGuid(),
            DateTime.UtcNow,
            new UserId(Id),
            newRole));
    }
}
