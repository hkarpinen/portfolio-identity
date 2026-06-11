using Application.Dtos;
using Application.Queries;
using Domain.Aggregates.User;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Queries;

internal sealed class UserQuery : IUserQuery
{
    private readonly IdentityDbContext _dbContext;

    public UserQuery(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return null;

        return new UserProfileDto(
            user.Id,
            user.Email!,
            user.DisplayName,
            user.AvatarUrl,
            user.Handle,
            user.Bio,
            user.Location,
            user.Pronouns,
            user.Role.ToString(),
            user.EmailConfirmed,
            user.TwoFactorEnabled,
            user.TwoFactorEnabledAt,
            user.CreatedAt);
    }

    public async Task<AdminUserListDto> ListUsersAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        // Exclude soft-deleted users from the admin list
        var query = _dbContext.Users.Where(u => u.DeletedAt == null);
        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .AsNoTracking()
            .OrderBy(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminUserDto(
                u.Id,
                u.Email ?? string.Empty,
                u.DisplayName,
                u.AvatarUrl,
                u.Role.ToString(),
                u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow,
                u.EmailConfirmed,
                u.CreatedAt))
            .ToListAsync(cancellationToken);

        return new AdminUserListDto(items, total);
    }
}
