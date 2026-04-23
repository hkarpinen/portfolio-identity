using Application.Managers;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IIdentityManager, IdentityManager>();
        return services;
    }
}
