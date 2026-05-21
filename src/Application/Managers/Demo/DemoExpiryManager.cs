using Application.Repositories;

namespace Identity.Application.Managers.Demo;

internal sealed class DemoExpiryManager : IDemoExpiryManager
{
    private readonly IUserRepository _userRepository;

    public DemoExpiryManager(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task ExpireAllAsync(CancellationToken cancellationToken = default)
    {
        var expired = await _userRepository.GetExpiredDemoUsersAsync(cancellationToken);
        foreach (var user in expired)
        {
            user.ExpireDemo();
            await _userRepository.SaveAsync(user, cancellationToken);
        }
    }
}
