using BakeTrack.Models;
using Microsoft.AspNetCore.Identity;

namespace BakeTrack.Data;

/// <summary>
/// Seeds application roles and initial user accounts safely across Development and Production.
/// </summary>
public static class IdentitySeeder
{
    public static async Task SeedAsync(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger logger)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole(role));
                if (!roleResult.Succeeded)
                {
                    logger.LogError(
                        "Failed to create role {Role}: {Errors}",
                        role,
                        string.Join("; ", roleResult.Errors.Select(e => e.Description)));
                }
            }
        }

        var adminEmail = FirstNonEmpty(configuration["ADMIN_EMAIL"], configuration["SeedAdmin:Email"]);
        var adminPassword = FirstNonEmpty(configuration["ADMIN_PASSWORD"], configuration["SeedAdmin:Password"]);
        var adminFullName = FirstNonEmpty(configuration["ADMIN_FULL_NAME"], configuration["SeedAdmin:FullName"]) ?? "Bakery Admin";

        if (environment.IsDevelopment())
        {
            adminEmail ??= "admin@baketrack.local";
            adminPassword ??= "Admin123!";

            await EnsureUserAsync(
                userManager,
                logger,
                email: adminEmail,
                password: adminPassword,
                fullName: adminFullName,
                role: AppRoles.Admin);

            var staffEmail = FirstNonEmpty(configuration["STAFF_EMAIL"], configuration["SeedStaff:Email"]) ?? "staff@baketrack.local";
            var staffPassword = FirstNonEmpty(configuration["STAFF_PASSWORD"], configuration["SeedStaff:Password"]) ?? "Staff123!";
            var staffFullName = FirstNonEmpty(configuration["STAFF_FULL_NAME"], configuration["SeedStaff:FullName"]) ?? "Bakery Staff";

            await EnsureUserAsync(
                userManager,
                logger,
                email: staffEmail,
                password: staffPassword,
                fullName: staffFullName,
                role: AppRoles.Staff);

            return;
        }

        // Production: bootstrap the initial Admin only when explicitly configured via environment variables.
        if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
        {
            await EnsureUserAsync(
                userManager,
                logger,
                email: adminEmail,
                password: adminPassword,
                fullName: adminFullName,
                role: AppRoles.Admin);
        }
        else
        {
            var existingAdmins = await userManager.GetUsersInRoleAsync(AppRoles.Admin);
            if (!existingAdmins.Any(u => u.IsActive))
            {
                logger.LogWarning(
                    "No active Admin user exists and ADMIN_EMAIL / ADMIN_PASSWORD are not configured. " +
                    "Set ADMIN_EMAIL and ADMIN_PASSWORD environment variables to seed an initial administrator.");
            }
        }
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        string email,
        string password,
        string fullName,
        string role)
    {
        var normalizedEmail = email.Trim();
        var user = await userManager.FindByEmailAsync(normalizedEmail);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = normalizedEmail,
                Email = normalizedEmail,
                FullName = fullName.Trim(),
                EmailConfirmed = true,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                logger.LogError(
                    "Failed to seed {Role} user '{Email}': {Errors}",
                    role,
                    normalizedEmail,
                    string.Join("; ", createResult.Errors.Select(e => e.Description)));
                return;
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            var roleResult = await userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                logger.LogError(
                    "Failed to assign role {Role} to '{Email}': {Errors}",
                    role,
                    normalizedEmail,
                    string.Join("; ", roleResult.Errors.Select(e => e.Description)));
            }
        }
    }

    private static string? FirstNonEmpty(params string?[] candidates)
        => candidates.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();
}
