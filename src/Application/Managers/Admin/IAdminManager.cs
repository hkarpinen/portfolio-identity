using Application;

namespace Identity.Application.Managers.Admin;

public interface IAdminManager
{
    /// <param name="reason">Why, in the admin's own words. Optional — a ban with no explanation
    /// is still a valid ban.</param>
    Task<Result> BanAsync(Guid userId, string? reason = null);
    Task<Result> UnbanAsync(Guid userId);
    Task<Result> ChangeRoleAsync(Guid userId, string role);
}
