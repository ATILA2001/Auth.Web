using Auth.Web.Data.Entities;

namespace Auth.Web.Services.Abstractions.Auth;

public interface IUserManagementService
{
    Task<ApplicationUser?> FindByNameAsync(string userName);
    Task<ApplicationUser?> FindByEmailAsync(string email);
    Task<IList<string>> GetRolesAsync(ApplicationUser user);
}
