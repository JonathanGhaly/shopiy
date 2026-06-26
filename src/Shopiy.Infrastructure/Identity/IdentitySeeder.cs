using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Shopiy.Domain.Entities;

namespace Shopiy.Infrastructure.Identity;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var roleManager =
            scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        var userManager =
            scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // -----------------------------
        // Roles
        // -----------------------------

        string[] roles =
        {
            "Admin",
            "Customer"
        };

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole
                {
                    Name = roleName,
                    NormalizedName = roleName.ToUpper(),
                    Description = $"{roleName} role"
                });
            }
        }

        // -----------------------------
        // Default Admin
        // -----------------------------

        const string adminEmail = "admin@shopiy.com";

        var admin = await userManager.FindByEmailAsync(adminEmail);

        if (admin is null)
        {
            admin = new ApplicationUser
            {
                FullName = "System Administrator",
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                EmailVerifiedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(
                admin,
                "Admin@12345");

            if (!result.Succeeded)
            {
                var errors = string.Join(
                    Environment.NewLine,
                    result.Errors.Select(x => x.Description));

                throw new InvalidOperationException(errors);
            }

            await userManager.AddToRoleAsync(admin, "Admin");
        }
    }
}
