using Application.Contracts;
using Application.Queries;
using Domain.Aggregates.User;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Queries;

internal sealed class IdentityQuery : IIdentityQuery
{
    private readonly IdentityDbContext _dbContext;

    public IdentityQuery(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserProfileResponse?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return null;

        return new UserProfileResponse(
            user.Id,
            user.Email!,
            user.DisplayName,
            user.AvatarUrl,
            user.Role.ToString(),
            user.EmailConfirmed,
            user.TwoFactorEnabled,
            user.CreatedAt);
    }

    public async Task<(IReadOnlyList<AdminUserResponse> Items, int Total)> ListUsersAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var total = await _dbContext.Users.CountAsync(cancellationToken);

        var items = await _dbContext.Users
            .AsNoTracking()
            .OrderBy(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminUserResponse(
                u.Id,
                u.Email ?? string.Empty,
                u.DisplayName,
                u.AvatarUrl,
                u.Role.ToString(),
                u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow,
                u.EmailConfirmed,
                u.CreatedAt))
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
