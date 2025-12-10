using Auth.Web.Data.Entities;

namespace Auth.Web.Services.Abstractions.Users;

public interface IUserProvisioningService
{
    Task<ApplicationUser> EnsureUserAsync(string userNameOrEmail, CancellationToken ct = default);
}
