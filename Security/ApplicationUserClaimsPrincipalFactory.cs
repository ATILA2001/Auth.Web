using System.Security.Claims;
using Auth.Web.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Auth.Web.Security;

public sealed class ApplicationUserClaimsPrincipalFactory
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
{
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
        var userNameClaimType = Options.ClaimsIdentity.UserNameClaimType;
        var displayName = GetDisplayName(user);

        foreach (var claim in identity.FindAll(userNameClaimType).ToList())
        {
            identity.RemoveClaim(claim);
        }

        identity.AddClaim(new Claim(userNameClaimType, displayName));
        return identity;
    }

    private static string GetDisplayName(ApplicationUser user)
    {
        var displayName = !string.IsNullOrWhiteSpace(user.Nombre)
            ? user.Nombre
            : user.UserName;

        if (!string.IsNullOrWhiteSpace(displayName) && displayName.Contains('@'))
        {
            displayName = displayName.Split('@')[0];
        }

        return !string.IsNullOrWhiteSpace(displayName)
            ? displayName
            : user.Id;
    }
}
