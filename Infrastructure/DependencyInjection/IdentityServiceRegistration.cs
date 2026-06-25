using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shopiy.Infrastructure.Identity;
using Shopiy.Infrastructure.Persistence;
using System.Security.Claims;

namespace Shopiy.Infrastructure.DependencyInjection;

public static class IdentityServiceRegistration
{
    public static IServiceCollection AddIdentityServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            // -------------------------
            // Password
            // -------------------------

            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredUniqueChars = 1;

            // -------------------------
            // User
            // -------------------------

            options.User.RequireUniqueEmail = true;

            // -------------------------
            // Sign In
            // -------------------------

            options.SignIn.RequireConfirmedEmail = true;

            // -------------------------
            // Lockout
            // -------------------------

            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan =
                TimeSpan.FromMinutes(15);

            // -------------------------
            // Tokens
            // -------------------------

            options.Tokens.EmailConfirmationTokenProvider =
                TokenOptions.DefaultEmailProvider;

            options.Tokens.PasswordResetTokenProvider =
                TokenOptions.DefaultProvider;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        services.Configure<IdentityOptions>(options =>
        {
            options.ClaimsIdentity.UserIdClaimType =
                ClaimTypes.NameIdentifier;

            options.ClaimsIdentity.UserNameClaimType =
                ClaimTypes.Name;

            options.ClaimsIdentity.EmailClaimType =
                ClaimTypes.Email;

            options.ClaimsIdentity.RoleClaimType =
                ClaimTypes.Role;
        });

        return services;
    }
}
