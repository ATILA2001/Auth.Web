using Auth.Web.Application.Auth;
using Auth.Web.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Auth.Web.Infrastructure.Users;

public sealed class UserManagementService : IUserManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserManagementService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public Task<ApplicationUser?> FindByNameAsync(string userName)
        => _userManager.FindByNameAsync(userName);

    public Task<ApplicationUser?> FindByEmailAsync(string email)
        => _userManager.FindByEmailAsync(email);

    public Task<IList<string>> GetRolesAsync(ApplicationUser user)
        => _userManager.GetRolesAsync(user);
}