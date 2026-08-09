using Application.Commands;
using Application.Ports;
using Application.Repositories;
using Domain.Aggregates.User;
using Identity.Application.Managers.TwoFactor;

namespace Tests;

// Reading the recovery-code COUNT and MINTING a new set are two different operations, and these
// tests are what stop them collapsing back into one — a read that regenerates would invalidate
// every code the user has written down just by opening the screen.
//
// No mocking library here, so the repository port is a hand-rolled fake that counts its own calls.
public class TwoFactorManagerTests
{
    private static AppUser EnabledUser()
    {
        var user = AppUser.Create(Email.From("user@example.com"), "User");
        user.MarkTwoFactorEnabled();
        return user;
    }

    [Fact]
    public async Task GetRecoveryCodeStatus_ShouldNotMintNewCodes()
    {
        var repo = new FakeUserRepository(EnabledUser()) { RecoveryCodeCount = 7 };
        var manager = new TwoFactorManager(repo, new StubJwtTokenGenerator());

        var result = await manager.GetRecoveryCodeStatusAsync(repo.User!.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value!.Remaining);
        // The whole point: reading the count is not a write.
        Assert.Equal(0, repo.GenerateRecoveryCodesCalls);
    }

    [Fact]
    public async Task GetRecoveryCodeStatus_ShouldReportZero_WhenTwoFactorIsOff()
    {
        // Not an error — the screen asks for the count on every visit, including
        // before 2FA is set up, and "you have none" is the truthful answer.
        var repo = new FakeUserRepository(AppUser.Create(Email.From("a@b.com"), "A"));
        var manager = new TwoFactorManager(repo, new StubJwtTokenGenerator());

        var result = await manager.GetRecoveryCodeStatusAsync(repo.User!.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.Remaining);
        Assert.Equal(0, repo.GenerateRecoveryCodesCalls);
    }

    [Fact]
    public async Task GenerateRecoveryCodes_ShouldMintExactlyOnce()
    {
        var repo = new FakeUserRepository(EnabledUser());
        var manager = new TwoFactorManager(repo, new StubJwtTokenGenerator());

        var result = await manager.GenerateRecoveryCodesAsync(repo.User!.Id);

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value!.Codes);
        Assert.Equal(1, repo.GenerateRecoveryCodesCalls);
    }

    [Fact]
    public async Task GenerateRecoveryCodes_ShouldRefuse_WhenTwoFactorIsOff()
    {
        var repo = new FakeUserRepository(AppUser.Create(Email.From("a@b.com"), "A"));
        var manager = new TwoFactorManager(repo, new StubJwtTokenGenerator());

        var result = await manager.GenerateRecoveryCodesAsync(repo.User!.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(0, repo.GenerateRecoveryCodesCalls);
    }


    [Fact]
    public async Task Confirm_ShouldTurnItOn_WhenTheCodeIsValid()
    {
        var user = AppUser.Create(Email.From("a@b.com"), "A");
        var repo = new FakeUserRepository(user) { TwoFactorTokenValid = true };
        var manager = new TwoFactorManager(repo, new StubJwtTokenGenerator());

        var result = await manager.ConfirmTwoFactorAsync(user.Id, new ConfirmTwoFactorCommand("123456"));

        Assert.True(result.IsSuccess);
        Assert.True(user.TwoFactorEnabled);
        Assert.NotNull(user.TwoFactorEnabledAt);
        Assert.True(repo.Saved);
    }

    [Fact]
    public async Task Confirm_ShouldRefuse_WhenTheCodeIsWrong()
    {
        // Otherwise a user could "enable" 2FA without the app ever holding the
        // key, and lock themselves out at the next sign-in.
        var user = AppUser.Create(Email.From("a@b.com"), "A");
        var repo = new FakeUserRepository(user) { TwoFactorTokenValid = false };
        var manager = new TwoFactorManager(repo, new StubJwtTokenGenerator());

        var result = await manager.ConfirmTwoFactorAsync(user.Id, new ConfirmTwoFactorCommand("000000"));

        Assert.False(result.IsSuccess);
        Assert.False(user.TwoFactorEnabled);
        Assert.False(repo.Saved);
    }

    [Fact]
    public async Task Confirm_ShouldBeIdempotent_WhenAlreadyOn()
    {
        var repo = new FakeUserRepository(EnabledUser()) { TwoFactorTokenValid = true };
        var manager = new TwoFactorManager(repo, new StubJwtTokenGenerator());
        var before = repo.User!.TwoFactorEnabledAt;

        var result = await manager.ConfirmTwoFactorAsync(repo.User.Id, new ConfirmTwoFactorCommand("123456"));

        Assert.True(result.IsSuccess);
        // Confirming twice must not silently reset the set-up date to today.
        Assert.Equal(before, repo.User.TwoFactorEnabledAt);
        Assert.False(repo.Saved);
    }

    [Fact]
    public async Task Disable_ShouldClearFlagAndTimestamp_WhenCodeIsValid()
    {
        var repo = new FakeUserRepository(EnabledUser()) { TwoFactorTokenValid = true };
        var manager = new TwoFactorManager(repo, new StubJwtTokenGenerator());

        var result = await manager.DisableTwoFactorAsync(
            repo.User!.Id, new DisableTwoFactorCommand("123456"));

        Assert.True(result.IsSuccess);
        Assert.False(repo.User.TwoFactorEnabled);
        Assert.Null(repo.User.TwoFactorEnabledAt);
        Assert.True(repo.Saved);
    }

    [Fact]
    public async Task Disable_ShouldRefuse_WhenCodeIsWrong()
    {
        var repo = new FakeUserRepository(EnabledUser()) { TwoFactorTokenValid = false };
        var manager = new TwoFactorManager(repo, new StubJwtTokenGenerator());

        var result = await manager.DisableTwoFactorAsync(
            repo.User!.Id, new DisableTwoFactorCommand("000000"));

        Assert.False(result.IsSuccess);
        Assert.True(repo.User.TwoFactorEnabled);
        Assert.False(repo.Saved);
    }
}

internal sealed class FakeUserRepository : IUserRepository
{
    public FakeUserRepository(AppUser user) => User = user;

    public AppUser? User { get; }
    public int RecoveryCodeCount { get; set; }
    public bool TwoFactorTokenValid { get; set; }
    public int GenerateRecoveryCodesCalls { get; private set; }
    public bool Saved { get; private set; }

    public Task<AppUser?> GetByIdAsync(UserId id, CancellationToken ct = default) =>
        Task.FromResult(User);

    public Task<AppUser?> GetByEmailAsync(Email email, CancellationToken ct = default) =>
        Task.FromResult(User);

    public Task SaveAsync(AppUser user, CancellationToken ct = default)
    {
        Saved = true;
        return Task.CompletedTask;
    }

    public Task<int> CountRecoveryCodesAsync(AppUser user, CancellationToken ct = default) =>
        Task.FromResult(RecoveryCodeCount);

    public Task<IReadOnlyList<string>> GenerateRecoveryCodesAsync(
        AppUser user, int count = 10, CancellationToken ct = default)
    {
        GenerateRecoveryCodesCalls++;
        IReadOnlyList<string> codes = Enumerable.Range(0, count).Select(i => $"code-{i}").ToList();
        return Task.FromResult(codes);
    }

    public Task<bool> VerifyTwoFactorTokenAsync(AppUser user, string code, CancellationToken ct = default) =>
        Task.FromResult(TwoFactorTokenValid);

    public Task<(bool, string?)> CreateWithPasswordAsync(AppUser u, string p, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<(bool, string?)> CreateDemoAsync(AppUser u, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<string> GenerateEmailConfirmationTokenAsync(AppUser u, CancellationToken ct = default) => throw new NotSupportedException();
    public Task QueueConfirmationEmailAsync(AppUser u, string t, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<(bool, string?)> ConfirmEmailAsync(AppUser u, string t, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<string> GeneratePasswordResetTokenAsync(AppUser u, CancellationToken ct = default) => throw new NotSupportedException();
    public Task QueuePasswordResetEmailAsync(AppUser u, string t, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<(bool, string?)> ResetPasswordAsync(AppUser u, string t, string p, CancellationToken ct = default) => throw new NotSupportedException();
    public Task ResetAuthenticatorKeyAsync(AppUser u, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<string?> GetAuthenticatorKeyAsync(AppUser u, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<(bool, string?)> UpdateAsync(AppUser u, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<IReadOnlyList<AppUser>> GetExpiredDemoUsersAsync(CancellationToken ct = default) => throw new NotSupportedException();
    public Task<bool> CheckPasswordAsync(AppUser u, string p, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<(bool, string?)> ChangePasswordAsync(AppUser u, string c, string n, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<(bool, string?)> ChangeEmailAsync(AppUser u, string e, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<IReadOnlyList<(string, string)>> GetExternalLoginsAsync(AppUser u, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<(bool, string?)> RemoveExternalLoginAsync(AppUser u, string p, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<int> CountPasswordsAndLoginsAsync(AppUser u, CancellationToken ct = default) => throw new NotSupportedException();
}

internal sealed class StubJwtTokenGenerator : IJwtTokenGenerator
{
    public TokenResult GenerateToken(AppUser user, DateTimeOffset? overrideExpiry = null) =>
        new("stub-token", overrideExpiry ?? DateTimeOffset.UtcNow.AddHours(1));
}
