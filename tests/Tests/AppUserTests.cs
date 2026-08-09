using Domain.Aggregates.User;
using Domain.Events;

namespace Tests;

public class AppUserTests
{
    [Fact]
    public void Create_ShouldReturnUser_WithCorrectProperties()
    {
        var email = Email.From("alice@example.com");
        var displayName = "Alice";

        var user = AppUser.Create(email, displayName);

        Assert.Equal("alice@example.com", user.Email);
        Assert.Equal(displayName, user.DisplayName);
        Assert.Equal(UserRole.Member, user.Role);
    }

    [Fact]
    public void Create_ShouldRaise_UserRegisteredEvent()
    {
        var email = Email.From("bob@example.com");

        var user = AppUser.Create(email, "Bob");

        Assert.Single(user.DomainEvents);
        Assert.IsType<UserRegistered>(user.DomainEvents[0]);
    }

    [Fact]
    public void Create_WithAdminRole_ShouldSetRoleToAdmin()
    {
        var email = Email.From("admin@example.com");

        var user = AppUser.Create(email, "Admin", UserRole.Admin);

        Assert.Equal(UserRole.Admin, user.Role);
    }

    [Fact]
    public void UpdateProfile_ShouldChangeDisplayNameAndAvatar()
    {
        var user = AppUser.Create(Email.From("test@example.com"), "Test");
        user.ClearDomainEvents();

        user.UpdateProfile("Updated Name", "https://example.com/avatar.jpg");

        Assert.Equal("Updated Name", user.DisplayName);
        Assert.Equal("https://example.com/avatar.jpg", user.AvatarUrl);
    }

    [Fact]
    public void UpdateProfile_ShouldRaise_UserProfileUpdatedEvent()
    {
        var user = AppUser.Create(Email.From("test@example.com"), "Test");
        user.ClearDomainEvents();

        user.UpdateProfile("New Name", null);

        Assert.Single(user.DomainEvents);
        Assert.IsType<UserProfileUpdated>(user.DomainEvents[0]);
    }

    [Fact]
    public void Ban_ShouldEnableLockout_WithMaxValue()
    {
        var user = AppUser.Create(Email.From("user@example.com"), "User");
        user.ClearDomainEvents();

        user.Ban();

        Assert.True(user.LockoutEnabled);
        Assert.Equal(DateTimeOffset.MaxValue, user.LockoutEnd);
    }

    [Fact]
    public void Ban_ShouldRaise_UserBannedEvent()
    {
        var user = AppUser.Create(Email.From("user@example.com"), "User");
        user.ClearDomainEvents();

        user.Ban();

        Assert.Single(user.DomainEvents);
        Assert.IsType<UserBanned>(user.DomainEvents[0]);
    }

    [Fact]
    public void ChangeRole_ShouldUpdateRole()
    {
        var user = AppUser.Create(Email.From("user@example.com"), "User");
        user.ClearDomainEvents();

        user.ChangeRole(UserRole.Admin);

        Assert.Equal(UserRole.Admin, user.Role);
    }

    [Fact]
    public void ChangeRole_ShouldRaise_UserRoleChangedEvent()
    {
        var user = AppUser.Create(Email.From("user@example.com"), "User");
        user.ClearDomainEvents();

        user.ChangeRole(UserRole.Admin);

        Assert.Single(user.DomainEvents);
        Assert.IsType<UserRoleChanged>(user.DomainEvents[0]);
    }

    [Fact]
    public void ClearDomainEvents_ShouldEmptyEventList()
    {
        var user = AppUser.Create(Email.From("user@example.com"), "User");
        Assert.NotEmpty(user.DomainEvents);

        user.ClearDomainEvents();

        Assert.Empty(user.DomainEvents);
    }

    [Fact]
    public void SoftDelete_ShouldAnonymiseAndLockout()
    {
        var user = AppUser.Create(Email.From("user@example.com"), "User");
        user.ClearDomainEvents();

        user.SoftDelete();

        Assert.NotNull(user.DeletedAt);
        Assert.Equal("[deleted]", user.DisplayName);
        Assert.True(user.LockoutEnabled);
        Assert.Equal(DateTimeOffset.MaxValue, user.LockoutEnd);
        Assert.False(user.EmailConfirmed);
    }

    [Fact]
    public void SoftDelete_ShouldRaise_UserAccountDeletedEvent()
    {
        var user = AppUser.Create(Email.From("user@example.com"), "User");
        user.ClearDomainEvents();

        user.SoftDelete();

        Assert.Single(user.DomainEvents);
        Assert.IsType<UserAccountDeleted>(user.DomainEvents[0]);
    }

    [Fact]
    public void MarkTwoFactorEnabled_ShouldSetFlagAndTimestampTogether()
    {
        var user = AppUser.Create(Email.From("user@example.com"), "User");

        user.MarkTwoFactorEnabled();

        Assert.True(user.TwoFactorEnabled);
        Assert.NotNull(user.TwoFactorEnabledAt);
    }

    [Fact]
    public void MarkTwoFactorDisabled_ShouldClearFlagAndTimestampTogether()
    {
        var user = AppUser.Create(Email.From("user@example.com"), "User");
        user.MarkTwoFactorEnabled();

        user.MarkTwoFactorDisabled();

        Assert.False(user.TwoFactorEnabled);
        Assert.Null(user.TwoFactorEnabledAt);
    }

    [Fact]
    public void Ban_ShouldRecordWhenAndWhy()
    {
        // A lockout must record WHEN and WHY, not only that it happened.
        var user = AppUser.Create(Email.From("spam@example.com"), "Spammer");

        user.Ban("repeat advertising");

        Assert.NotNull(user.BannedAt);
        Assert.Equal("repeat advertising", user.BanReason);
        Assert.Equal(DateTimeOffset.MaxValue, user.LockoutEnd);
    }

    [Fact]
    public void Ban_ShouldAllowNoReason()
    {
        var user = AppUser.Create(Email.From("spam@example.com"), "Spammer");

        user.Ban();

        Assert.NotNull(user.BannedAt);
        Assert.Null(user.BanReason);
    }

    [Fact]
    public void Ban_ShouldTreatBlankReasonAsNone()
    {
        // Otherwise the row renders "Locked out Jul 24 — " with a dangling dash.
        var user = AppUser.Create(Email.From("spam@example.com"), "Spammer");

        user.Ban("   ");

        Assert.Null(user.BanReason);
    }

    [Fact]
    public void Unban_ShouldClearTheRecordWithTheLockout()
    {
        // A lifted ban must not leave a row reading "Locked out Jul 24" beside
        // an account that can sign in again.
        var user = AppUser.Create(Email.From("spam@example.com"), "Spammer");
        user.Ban("repeat advertising");

        user.Unban();

        Assert.Null(user.LockoutEnd);
        Assert.Null(user.BannedAt);
        Assert.Null(user.BanReason);
    }

    [Fact]
    public void RequestEmailReverification_ShouldUnconfirmAndRaiseEvent()
    {
        var user = AppUser.Create(Email.From("user@example.com"), "User");
        user.EmailConfirmed = true;
        user.ClearDomainEvents();

        user.RequestEmailReverification("confirm-token");

        Assert.False(user.EmailConfirmed);
        Assert.Single(user.DomainEvents);
        var evt = Assert.IsType<UserEmailConfirmationRequested>(user.DomainEvents[0]);
        Assert.Equal("confirm-token", evt.ConfirmationToken);
    }

    [Fact]
    public void Email_From_InvalidFormat_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => Email.From("not-an-email"));
    }

    [Fact]
    public void Email_From_EmptyString_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => Email.From(""));
    }
}
