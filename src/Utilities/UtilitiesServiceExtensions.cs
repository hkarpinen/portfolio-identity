using Microsoft.Extensions.DependencyInjection;

namespace Utilities;

public static class UtilitiesServiceExtensions
{
    public static IServiceCollection AddUtilities(this IServiceCollection services)
    {
        services.AddSingleton<IClock, Clock>();
        return services;
    }
}
