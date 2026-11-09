using BabyCareApp.Application.Abstractions;
using BabyCareApp.Application.Admin;
using BabyCareApp.Application.Authorization;
using BabyCareApp.Application.Auth;
using BabyCareApp.Application.Subscriptions;
using BabyCareApp.Application.Tenancy;
using BabyCareApp.Infrastructure.Admin;
using BabyCareApp.Infrastructure.Audit;
using BabyCareApp.Infrastructure.Auth;
using BabyCareApp.Infrastructure.Authorization;
using BabyCareApp.Infrastructure.Persistence;
using BabyCareApp.Infrastructure.Subscriptions;
using BabyCareApp.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BabyCareApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Missing 'ConnectionStrings:Postgres' configuration value.");

        // ITenantContext is registered by the Api layer (HttpTenantContext, Slice 4) —
        // Infrastructure only needs the interceptor that reads it.
        services.AddScoped<TenantConnectionInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(sp.GetRequiredService<TenantConnectionInterceptor>()));

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IAuditLogger, AuditLogger>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<ITenantService, TenantService>();

        return services;
    }
}
