
using ConnectGrowAPI.Data;
using ConnectGrowAPI.Models;
using Microsoft.AspNetCore.Identity;

namespace ConnectGrowAPI.Api.Data;


public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var config = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();

        foreach (var roleName in new[] { RoleNames.Admin, RoleNames.User })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole(roleName));
                logger.LogInformation("Seeded role {Role}.", roleName);
            }
        }

        var adminEmail = config["Seed:AdminEmail"];
        var adminPassword = config["Seed:AdminPassword"];

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning(
                "Seed:AdminEmail / Seed:AdminPassword not configured — no admin account created.");
            return;
        }

        if (await userManager.FindByEmailAsync(adminEmail) is not null)
            return;

        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            FirstName = config["Seed:AdminFirstName"] ?? "Practice",
            LastName = config["Seed:AdminLastName"] ?? "Admin",
            IsActive = true
        };

        var result = await userManager.CreateAsync(admin, adminPassword);

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, RoleNames.Admin);
            logger.LogInformation("Seeded admin account {Email}.", adminEmail);
        }
        else
        {
            logger.LogError(
                "Failed to seed admin account: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }
}