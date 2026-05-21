namespace Identity.Application.Managers.Demo;

public interface IDemoExpiryManager
{
    Task ExpireAllAsync(CancellationToken cancellationToken = default);
}
