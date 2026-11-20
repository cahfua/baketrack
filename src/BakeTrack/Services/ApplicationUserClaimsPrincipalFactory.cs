using System.Security.Claims;
using BakeTrack.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace BakeTrack.Services;

/// <summary>
/// Adds custom profile claims (such as FullName) to the authenticated ClaimsPrincipal.
/// </summary>
public sealed class ApplicationUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
{
    public const string FullNameClaimType = "full_name";

    public ApplicationUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, roleManager, optionsAccessor)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        if (!string.IsNullOrWhiteSpace(user.FullName))
        {
            identity.AddClaim(new Claim(FullNameClaimType, user.FullName));
        }

        return identity;
    }
}
