using System.Threading;
using System.Threading.Tasks;
using Auth.Web.Data.Entities;
using Auth.Web.Services.Abstractions.Auth;
using Microsoft.AspNetCore.Identity;

namespace Auth.Web.Services.Implementations.Auth;

public sealed class AdminSignInService : IAdminSignInService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AdminSignInService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task SignInAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return;
        }

        await _signInManager.SignInAsync(user, isPersistent: false);
    }
}
