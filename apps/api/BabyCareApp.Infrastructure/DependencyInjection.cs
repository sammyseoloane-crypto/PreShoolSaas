using BabyCareApp.Application.Abstractions;
using BabyCareApp.Application.Auth;
using BabyCareApp.Infrastructure.Auth;
using BabyCareApp.Infrastructure.Persistence;
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

        // Replaced by an HTTP-request-scoped implementation in Slice 4 (Tenant Management).
        services.AddScoped<ITenantContext, NullTenantContext>();
        services.AddScoped<TenantConnectionInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(sp.GetRequiredService<TenantConnectionInterceptor>()));

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
