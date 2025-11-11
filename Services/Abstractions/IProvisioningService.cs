using Auth.Web.Domain.Entities;

namespace Auth.Web.Services.Abstractions;

public interface IProvisioningService
{
    Task<ApplicationUser> EnsureUserAsync(string userName, string? displayName = null);
}
