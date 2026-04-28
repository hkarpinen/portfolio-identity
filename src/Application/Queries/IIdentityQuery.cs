using Application.Contracts;

namespace Application.Queries;

public interface IIdentityQuery
{
    Task<UserProfileResponse?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<AdminUserResponse> Items, int Total)> ListUsersAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}
