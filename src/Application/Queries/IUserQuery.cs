using Application.Dtos;

namespace Application.Queries;

public interface IUserQuery
{
    Task<UserProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<AdminUserDto> Items, int Total)> ListUsersAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}
