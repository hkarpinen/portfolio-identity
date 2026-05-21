using Application;
using Application.Dtos;

namespace Identity.Application.Managers.Demo;

public interface IDemoManager
{
    Task<Result<DemoStartDto>> StartDemoAsync(CancellationToken cancellationToken = default);
}
