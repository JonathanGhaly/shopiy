
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shopiy.Domain.Interfaces;
using Shopiy.Infrastructure.Identity;
using Shopiy.Infrastructure.Persistence;

namespace Shopiy.Infrastructure;

public static class PersistenceServiceRegistration
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(
                        typeof(ApplicationDbContext).Assembly.FullName);

                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null);
                });

#if DEBUG
            options.EnableDetailedErrors();
            options.EnableSensitiveDataLogging();
#endif
        });

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            // Password

            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredUniqueChars = 1;

            // User

            options.User.RequireUniqueEmail = true;

            // Sign In

            options.SignIn.RequireConfirmedEmail = true;

            // Lockout

            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan =
                TimeSpan.FromMinutes(15);
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        services.Configure<IdentityOptions>(options =>
        {
            options.ClaimsIdentity.UserIdClaimType = ClaimTypes.NameIdentifier;
            options.ClaimsIdentity.UserNameClaimType = ClaimTypes.Name;
            options.ClaimsIdentity.EmailClaimType = ClaimTypes.Email;
            options.ClaimsIdentity.RoleClaimType = ClaimTypes.Role;
        });

        return services;
    }
}