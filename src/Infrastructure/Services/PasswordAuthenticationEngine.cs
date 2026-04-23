using Application.Services;
using Domain.Aggregates.User;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Services;

internal sealed class PasswordAuthenticationEngine : IPasswordAuthenticationEngine
{
    private readonly SignInManager<AppUser> _signInManager;

    public PasswordAuthenticationEngine(SignInManager<AppUser> signInManager)
        => _signInManager = signInManager;

    public async Task<PasswordCheckResult> CheckPasswordAsync(AppUser user, string password, CancellationToken cancellationToken = default)
    {
        var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        return new PasswordCheckResult(result.Succeeded, result.IsLockedOut);
    }
}
