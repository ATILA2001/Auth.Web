using Auth.Web.Data.Entities;

namespace Auth.Web.Services.Abstractions.Clients;

public interface IClientService
{
    Task<ApplicationClient?> GetAsync(string clientId);
    Task<IReadOnlyList<ApplicationClient>> GetAllAsync(CancellationToken ct = default);
    bool IsReturnUrlAllowed(ApplicationClient client, string returnUrl);
    string? GetDefaultReturnUrl(ApplicationClient client);
    string? GetLandingUrl(ApplicationClient client, bool useClientDefaultLandingPage = true);
    string ResolveReturnUrlForCurrentEnvironment(ApplicationClient client, string returnUrl, string? appId = null);
}
