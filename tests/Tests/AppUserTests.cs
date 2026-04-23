using Domain.Aggregates.User;
using Domain.Events;

namespace Tests;

public class AppUserTests
{
    [Fact]
    public void Create_ShouldReturnUser_WithCorrectProperties()
    {
        // Arrange
        var email = Email.From("alice@example.com");
        var displayName = "Alice";

        // Act
        var user = AppUser.Create(email, displayName);

        // Assert
        Assert.Equal("alice@example.com", user.Email);
        Assert.Equal(displayName, user.DisplayName);
        Assert.Equal(UserRole.Member, user.Role);
    }

    [Fact]
    public void Create_ShouldRaise_UserRegisteredEvent()
    {
        // Arrange
        var email = Email.From("bob@example.com");

        // Act
        var user = AppUser.Create(email, "Bob");

        // Assert
        Assert.Single(user.DomainEvents);
        Assert.IsType<UserRegistered>(user.DomainEvents[0]);
    }

    [Fact]
    public void Create_WithAdminRole_ShouldSetRoleToAdmin()
    {
        // Arrange
        var email = Email.From("admin@example.com");

        // Act
        var user = AppUser.Create(email, "Admin", UserRole.Admin);

        // Assert
        Assert.Equal(UserRole.Admin, user.Role);
    }

    [Fact]
    public void UpdateProfile_ShouldChangeDisplayNameAndAvatar()
    {
        // Arrange
        var user = AppUser.Create(Email.From("test@example.com"), "Test");
        user.ClearDomainEvents();

        // Act
        user.UpdateProfile("Updated Name", "https://example.com/avatar.jpg");

        // Assert
        Assert.Equal("Updated Name", user.DisplayName);
        Assert.Equal("https://example.com/avatar.jpg", user.AvatarUrl);
    }

    [Fact]
    public void UpdateProfile_ShouldRaise_UserProfileUpdatedEvent()
    {
        // Arrange
        var user = AppUser.Create(Email.From("test@example.com"), "Test");
        user.ClearDomainEvents();

        // Act
        user.UpdateProfile("New Name", null);

        // Assert
        Assert.Single(user.DomainEvents);
        Assert.IsType<UserProfileUpdated>(user.DomainEvents[0]);
    }

    [Fact]
    public void Ban_ShouldEnableLockout_WithMaxValue()
    {
        // Arrange
        var user = AppUser.Create(Email.From("user@example.com"), "User");
        user.ClearDomainEvents();

        // Act
        user.Ban();

        // Assert
        Assert.True(user.LockoutEnabled);
        Assert.Equal(DateTimeOffset.MaxValue, user.LockoutEnd);
    }

    [Fact]
    public void Ban_ShouldRaise_UserBannedEvent()
    {
        // Arrange
        var user = AppUser.Create(Email.From("user@example.com"), "User");
        user.ClearDomainEvents();

        // Act
        user.Ban();

        // Assert
        Assert.Single(user.DomainEvents);
        Assert.IsType<UserBanned>(user.DomainEvents[0]);
    }

    [Fact]
    public void ChangeRole_ShouldUpdateRole()
    {
        // Arrange
        var user = AppUser.Create(Email.From("user@example.com"), "User");
        user.ClearDomainEvents();

        // Act
        user.ChangeRole(UserRole.Admin);

        // Assert
        Assert.Equal(UserRole.Admin, user.Role);
    }

    [Fact]
    public void ChangeRole_ShouldRaise_UserRoleChangedEvent()
    {
        // Arrange
        var user = AppUser.Create(Email.From("user@example.com"), "User");
        user.ClearDomainEvents();

        // Act
        user.ChangeRole(UserRole.Admin);

        // Assert
        Assert.Single(user.DomainEvents);
        Assert.IsType<UserRoleChanged>(user.DomainEvents[0]);
    }

    [Fact]
    public void ClearDomainEvents_ShouldEmptyEventList()
    {
        // Arrange
        var user = AppUser.Create(Email.From("user@example.com"), "User");
        Assert.NotEmpty(user.DomainEvents);

        // Act
        user.ClearDomainEvents();

        // Assert
        Assert.Empty(user.DomainEvents);
    }

    [Fact]
    public void Email_From_InvalidFormat_ShouldThrow()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentException>(() => Email.From("not-an-email"));
    }

    [Fact]
    public void Email_From_EmptyString_ShouldThrow()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentException>(() => Email.From(""));
    }
}
