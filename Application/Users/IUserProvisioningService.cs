using Auth.Web.Domain.Entities;

namespace Auth.Web.Application.Users;

public interface IUserProvisioningService
{
    Task<ApplicationUser> EnsureUserAsync(string userNameOrEmail, CancellationToken ct = default);
}