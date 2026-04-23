using Auth.Web.Data.Entities;

namespace Auth.Web.Services.Abstractions.Users;

public interface IUserProvisioningService
{
    Task<ApplicationUser> EnsureUserAsync(string userName, string? email = null, string? nombre = null, CancellationToken ct = default);
}
