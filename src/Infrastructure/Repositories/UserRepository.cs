using Domain.Aggregates.User;
using Domain.Repositories;
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

    public async Task<(IReadOnlyList<AppUser> Items, int Total)> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var total = await _dbContext.Users.CountAsync(cancellationToken);
        var items = await _dbContext.Users
            .OrderBy(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

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

    public async Task<(bool Succeeded, string? Error)> ConfirmEmailAsync(AppUser user, string token, CancellationToken cancellationToken = default)
    {
        var result = await _userManager.ConfirmEmailAsync(user, token);
        return (result.Succeeded, result.Succeeded ? null : Describe(result));
    }

    public Task ResetAuthenticatorKeyAsync(AppUser user, CancellationToken cancellationToken = default)
        => _userManager.ResetAuthenticatorKeyAsync(user);

    public Task<string?> GetAuthenticatorKeyAsync(AppUser user, CancellationToken cancellationToken = default)
        => _userManager.GetAuthenticatorKeyAsync(user);

    public Task<bool> VerifyTwoFactorTokenAsync(AppUser user, string code, CancellationToken cancellationToken = default)
        => _userManager.VerifyTwoFactorTokenAsync(user, _userManager.Options.Tokens.AuthenticatorTokenProvider, code);

    public Task SetTwoFactorEnabledAsync(AppUser user, bool enabled, CancellationToken cancellationToken = default)
        => _userManager.SetTwoFactorEnabledAsync(user, enabled);

    public async Task<(bool Succeeded, string? Error)> UpdateAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        var result = await _userManager.UpdateAsync(user);
        return (result.Succeeded, result.Succeeded ? null : Describe(result));
    }

    private static string Describe(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(e => e.Description));
}

