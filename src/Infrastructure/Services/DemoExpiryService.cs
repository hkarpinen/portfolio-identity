using Identity.Application.Managers.Demo;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

internal sealed class DemoExpiryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DemoExpiryService> _logger;

    public DemoExpiryService(IServiceScopeFactory scopeFactory, ILogger<DemoExpiryService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var manager = scope.ServiceProvider.GetRequiredService<IDemoExpiryManager>();
                await manager.ExpireAllAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error expiring demo users");
            }

            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }
}
