using Application;

namespace Identity.Application.Managers.Admin;

public interface IAdminManager
{
    Task<Result> BanAsync(Guid userId);
    Task<Result> ChangeRoleAsync(Guid userId, string role);
}
