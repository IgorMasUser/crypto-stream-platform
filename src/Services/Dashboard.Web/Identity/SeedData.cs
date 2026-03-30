using Microsoft.AspNetCore.Identity;

namespace TradingApp.Dashboard.Web.Identity;

public static class SeedData
{
    public static async Task SeedAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration)
    {
        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var adminEmail    = configuration["Auth:Admin:Email"]    ?? "admin@tradingapp.local";
        var adminPassword = configuration["Auth:Admin:Password"] ?? "Admin123!";
        var adminDisplay  = configuration["Auth:Admin:DisplayName"] ?? "Administrator";

        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName    = adminEmail,
                Email       = adminEmail,
                DisplayName = adminDisplay,
                EmailConfirmed = true,
            };

            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, AppRoles.Admin);
        }
    }
}
