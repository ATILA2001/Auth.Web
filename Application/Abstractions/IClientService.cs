using Auth.Web.Domain.Entities;

namespace Auth.Web.Application.Abstractions;

public interface IClientService
{
    Task<ApplicationClient?> GetAsync(string clientId);
    bool IsReturnUrlAllowed(ApplicationClient client, string returnUrl);
}
