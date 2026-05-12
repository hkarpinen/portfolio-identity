using Application;
using Application.Repositories;
using Domain.Aggregates.User;

namespace Identity.Application.Managers.Admin;

internal sealed class AdminManager : IAdminManager
{
    private readonly IUserRepository _userRepository;

    public AdminManager(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result> BanAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(new UserId(userId));

        if (user is null)
            return Result.Failure("User not found.");

        user.Ban();

        await _userRepository.SaveAsync(user);
        return Result.Success();
    }

    public async Task<Result> ChangeRoleAsync(Guid userId, string role)
    {
        var user = await _userRepository.GetByIdAsync(new UserId(userId));
        if (user is null)
            return Result.Failure("User not found.");

        if (!Enum.TryParse<UserRole>(role, ignoreCase: true, out var newRole))
            return Result.Failure($"Unknown role '{role}'.");

        user.ChangeRole(newRole);
        await _userRepository.SaveAsync(user);
        return Result.Success();
    }
}
