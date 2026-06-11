using Domain.Aggregates.User;
using Application.Repositories;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

internal sealed class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _dbContext;
    private readonly UserManager<AppUser> _userManager;

    public UserRepository(IdentityDbContext dbContext, UserManager<AppUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task<AppUser?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
        => await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id.Value, cancellationToken);

    public async Task<AppUser?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
        => await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email.Value, cancellationToken);

    public async Task SaveAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        // Persist Identity fields (roles, claims, lockout, etc.) via UserManager first
        // so the EF change tracker has all mutations before we flush the outbox.
        await _userManager.UpdateAsync(user);

        foreach (var domainEvent in user.DomainEvents)
            _dbContext.AddToOutbox(domainEvent);

        user.ClearDomainEvents();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<(bool Succeeded, string? Error)> CreateWithPasswordAsync(AppUser user, string password, CancellationToken cancellationToken = default)
    {
        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return (false, Describe(result));

        // Flush domain events raised during creation (e.g. UserRegistered) into the outbox
        // so downstream services receive them once the outbox relay polls.
        foreach (var domainEvent in user.DomainEvents)
            _dbContext.AddToOutbox(domainEvent);
        user.ClearDomainEvents();
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (true, null);
    }

    public Task<string> GenerateEmailConfirmationTokenAsync(AppUser user, CancellationToken cancellationToken = default)
        => _userManager.GenerateEmailConfirmationTokenAsync(user);

    public async Task QueueConfirmationEmailAsync(AppUser user, string token, CancellationToken cancellationToken = default)
    {
        _dbContext.AddToOutbox(new Domain.Events.UserEmailConfirmationRequested(
            Guid.NewGuid(), DateTime.UtcNow, user.Id, user.Email!, user.DisplayName, token));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<(bool Succeeded, string? Error)> ConfirmEmailAsync(AppUser user, string token, CancellationToken cancellationToken = default)
    {
        var result = await _userManager.ConfirmEmailAsync(user, token);
        return (result.Succeeded, result.Succeeded ? null : Describe(result));
    }

    public Task<string> GeneratePasswordResetTokenAsync(AppUser user, CancellationToken cancellationToken = default)
        => _userManager.GeneratePasswordResetTokenAsync(user);

    public async Task QueuePasswordResetEmailAsync(AppUser user, string token, CancellationToken cancellationToken = default)
    {
        _dbContext.AddToOutbox(new Domain.Events.UserPasswordResetRequested(
            Guid.NewGuid(), DateTime.UtcNow, user.Id, user.Email!, user.DisplayName, token));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<(bool Succeeded, string? Error)> ResetPasswordAsync(AppUser user, string token, string newPassword, CancellationToken cancellationToken = default)
    {
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        return (result.Succeeded, result.Succeeded ? null : Describe(result));
    }

    public Task ResetAuthenticatorKeyAsync(AppUser user, CancellationToken cancellationToken = default)
        => _userManager.ResetAuthenticatorKeyAsync(user);

    public Task<string?> GetAuthenticatorKeyAsync(AppUser user, CancellationToken cancellationToken = default)
        => _userManager.GetAuthenticatorKeyAsync(user);

    public Task<bool> VerifyTwoFactorTokenAsync(AppUser user, string code, CancellationToken cancellationToken = default)
        => _userManager.VerifyTwoFactorTokenAsync(user, _userManager.Options.Tokens.AuthenticatorTokenProvider, code);

    public async Task<(bool Succeeded, string? Error)> UpdateAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        var result = await _userManager.UpdateAsync(user);
        return (result.Succeeded, result.Succeeded ? null : Describe(result));
    }

    public async Task<IReadOnlyList<AppUser>> GetExpiredDemoUsersAsync(CancellationToken cancellationToken = default)
        => await _dbContext.Users
            .Where(u => u.IsDemo && u.DemoExpiresAt < DateTime.UtcNow && u.DemoExpiredAt == null)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<string>> GenerateRecoveryCodesAsync(AppUser user, int count = 10, CancellationToken cancellationToken = default)
    {
        var codes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, count);
        return codes?.ToList().AsReadOnly() ?? (IReadOnlyList<string>)Array.Empty<string>();
    }
    public async Task<(bool Succeeded, string? Error)> CreateDemoAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        var result = await _userManager.CreateAsync(user);
        if (!result.Succeeded)
            return (false, Describe(result));

        foreach (var domainEvent in user.DomainEvents)
            _dbContext.AddToOutbox(domainEvent);
        user.ClearDomainEvents();
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (true, null);
    }

    private static string Describe(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(e => e.Description));
}

