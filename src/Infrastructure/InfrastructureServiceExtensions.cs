using Application.Ports;
using Application.Queries;
using Application.Repositories;
using Domain.Aggregates.User;
using Infrastructure.Messaging;
using Infrastructure.Persistence;
using Infrastructure.Queries;
using Infrastructure.Repositories;
using Infrastructure.Services;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(
                    configuration.GetConnectionString("Identity"),
                    npgsql => npgsql.MigrationsAssembly("Infrastructure"))
                .UseSnakeCaseNamingConvention());

        services.AddIdentity<AppUser, IdentityRole<Guid>>(options =>
            {
                options.SignIn.RequireConfirmedEmail = true;
                options.Password.RequiredLength = 12;
                options.Lockout.MaxFailedAccessAttempts = 5;
            })
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddDefaultTokenProviders();

        var rabbitConfig = configuration.GetSection("RabbitMq");
        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitConfig["Host"], h =>
                {
                    h.Username(rabbitConfig["Username"]!);
                    h.Password(rabbitConfig["Password"]!);
                });

                cfg.Publish<Domain.Events.UserRegistered>(p => p.Durable = true);
                cfg.Publish<Domain.Events.UserProfileUpdated>(p => p.Durable = true);
                cfg.Publish<Domain.Events.UserBanned>(p => p.Durable = true);
                cfg.Publish<Domain.Events.UserRoleChanged>(p => p.Durable = true);
                cfg.Publish<Domain.Events.DemoUserCreated>(p => p.Durable = true);
                cfg.Publish<Domain.Events.DemoUserExpired>(p => p.Durable = true);
                cfg.Publish<Domain.Events.UserEmailConfirmationRequested>(p => p.Durable = true);
            });
        });

        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.Configure<LocalFileStorageOptions>(configuration.GetSection("Storage"));
        services.Configure<RecaptchaOptions>(configuration.GetSection("Recaptcha"));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserQuery, UserQuery>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IPasswordAuthenticationEngine, PasswordAuthenticationEngine>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();

        services.AddHttpClient<IRecaptchaService, RecaptchaService>();

        services.AddHostedService<OutboxPublisher>();
        services.AddHostedService<DemoExpiryService>();

        return services;
    }
}
