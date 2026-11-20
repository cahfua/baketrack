using System.ComponentModel.DataAnnotations;
using BakeTrack.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BakeTrack.Services;

public sealed record UserSummary(
    string Id,
    string FullName,
    string Email,
    string Role,
    bool IsActive,
    bool IsLockedOut,
    DateTime CreatedAtUtc);

public sealed class CreateUserInput
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required.")]
    public string Role { get; set; } = AppRoles.Staff;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm the password.")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class UpdateUserInput
{
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required.")]
    public string Role { get; set; } = AppRoles.Staff;
}

public sealed class AdminResetPasswordInput
{
    public string UserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm the new password.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class ChangeOwnPasswordInput
{
    [Required(ErrorMessage = "Current password is required.")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your new password.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// Manages admin-controlled BakeTrack user accounts, roles, activation status, and password operations.
/// </summary>
public class UserManagementService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public UserManagementService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<List<UserSummary>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var users = await userManager.Users
            .AsNoTracking()
            .OrderBy(u => u.FullName)
            .ThenBy(u => u.Email)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var summaries = new List<UserSummary>(users.Count);

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            var primaryRole = roles.Contains(AppRoles.Admin)
                ? AppRoles.Admin
                : roles.FirstOrDefault() ?? AppRoles.Staff;

            var isLockedOut = user.LockoutEnabled && user.LockoutEnd.HasValue && user.LockoutEnd.Value > now;

            summaries.Add(new UserSummary(
                user.Id,
                string.IsNullOrWhiteSpace(user.FullName) ? (user.Email ?? string.Empty) : user.FullName,
                user.Email ?? string.Empty,
                primaryRole,
                user.IsActive,
                isLockedOut,
                user.CreatedAtUtc));
        }

        return summaries;
    }

    public async Task<OperationResult> CreateUserAsync(CreateUserInput input)
    {
        var fullName = input.FullName?.Trim() ?? string.Empty;
        var email = input.Email?.Trim() ?? string.Empty;
        var role = input.Role?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(fullName))
        {
            return new OperationResult(false, "Full name is required.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return new OperationResult(false, "Email is required.");
        }

        if (!AppRoles.All.Contains(role))
        {
            return new OperationResult(false, "Invalid role selected.");
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            return new OperationResult(false, $"An account with email '{email}' already exists.");
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            EmailConfirmed = true,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(user, input.Password);
        if (!createResult.Succeeded)
        {
            return new OperationResult(false, FormatIdentityErrors(createResult));
        }

        var roleResult = await userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return new OperationResult(false, FormatIdentityErrors(roleResult));
        }

        return new OperationResult(true);
    }

    public async Task<OperationResult> UpdateUserAsync(UpdateUserInput input, string currentAdminUserId)
    {
        var fullName = input.FullName?.Trim() ?? string.Empty;
        var newRole = input.Role?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(fullName))
        {
            return new OperationResult(false, "Full name is required.");
        }

        if (!AppRoles.All.Contains(newRole))
        {
            return new OperationResult(false, "Invalid role selected.");
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByIdAsync(input.Id);
        if (user is null)
        {
            return new OperationResult(false, "The selected user no longer exists.");
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        var wasAdmin = currentRoles.Contains(AppRoles.Admin);

        if (wasAdmin && newRole != AppRoles.Admin)
        {
            if (user.Id == currentAdminUserId)
            {
                return new OperationResult(false, "You cannot remove your own Admin role.");
            }

            if (await IsLastActiveAdminAsync(userManager, user.Id))
            {
                return new OperationResult(false, "Cannot demote the last active Admin account.");
            }
        }

        user.FullName = fullName;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return new OperationResult(false, FormatIdentityErrors(updateResult));
        }

        if (!currentRoles.Contains(newRole) || currentRoles.Count != 1)
        {
            if (currentRoles.Count > 0)
            {
                var removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    return new OperationResult(false, FormatIdentityErrors(removeResult));
                }
            }

            var addResult = await userManager.AddToRoleAsync(user, newRole);
            if (!addResult.Succeeded)
            {
                return new OperationResult(false, FormatIdentityErrors(addResult));
            }

            await userManager.UpdateSecurityStampAsync(user);
        }

        return new OperationResult(true);
    }

    public async Task<OperationResult> SetUserActiveAsync(string targetUserId, bool isActive, string currentAdminUserId)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByIdAsync(targetUserId);
        if (user is null)
        {
            return new OperationResult(false, "The selected user no longer exists.");
        }

        if (!isActive)
        {
            if (user.Id == currentAdminUserId)
            {
                return new OperationResult(false, "You cannot deactivate your own account.");
            }

            var isAdmin = await userManager.IsInRoleAsync(user, AppRoles.Admin);
            if (isAdmin && await IsLastActiveAdminAsync(userManager, user.Id))
            {
                return new OperationResult(false, "Cannot deactivate the last active Admin account.");
            }
        }

        user.IsActive = isActive;
        if (isActive)
        {
            user.LockoutEnd = null;
        }

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return new OperationResult(false, FormatIdentityErrors(result));
        }

        if (!isActive)
        {
            await userManager.UpdateSecurityStampAsync(user);
        }

        return new OperationResult(true);
    }

    public async Task<OperationResult> ResetPasswordAsync(string targetUserId, string newPassword)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByIdAsync(targetUserId);
        if (user is null)
        {
            return new OperationResult(false, "The selected user no longer exists.");
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            return new OperationResult(false, FormatIdentityErrors(result));
        }

        user.LockoutEnd = null;
        await userManager.UpdateAsync(user);
        await userManager.UpdateSecurityStampAsync(user);

        return new OperationResult(true);
    }

    public async Task<OperationResult> ChangeOwnPasswordAsync(string userId, string currentPassword, string newPassword)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return new OperationResult(false, "User account not found.");
        }

        var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
        {
            return new OperationResult(false, FormatIdentityErrors(result));
        }

        await userManager.UpdateSecurityStampAsync(user);
        return new OperationResult(true);
    }

    private static async Task<bool> IsLastActiveAdminAsync(UserManager<ApplicationUser> userManager, string excludingUserId)
    {
        var admins = await userManager.GetUsersInRoleAsync(AppRoles.Admin);
        return !admins.Any(a => a.IsActive && a.Id != excludingUserId);
    }

    private static string FormatIdentityErrors(IdentityResult result)
        => string.Join(" ", result.Errors.Select(e => e.Description));
}
