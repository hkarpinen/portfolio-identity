using Identity.Application.Managers.Admin;
using Identity.Application.Managers.Auth;
using Identity.Application.Managers.Demo;
using Identity.Application.Managers.Profile;
using Identity.Application.Managers.TwoFactor;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthManager, AuthManager>();
        services.AddScoped<ITwoFactorManager, TwoFactorManager>();
        services.AddScoped<IProfileManager, ProfileManager>();
        services.AddScoped<IAdminManager, AdminManager>();
        services.AddScoped<IDemoManager, DemoManager>();
        services.AddScoped<IDemoExpiryManager, DemoExpiryManager>();
        return services;
    }
}
