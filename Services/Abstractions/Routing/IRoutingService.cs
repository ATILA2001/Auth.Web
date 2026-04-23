namespace Auth.Web.Services.Abstractions.Routing;

public interface IRoutingService
{
    Task<(string ClientId, string ReturnUrl)?> ResolveForUserAsync(string userId, CancellationToken ct = default);
    Task<IReadOnlyList<(string ClientId, string ReturnUrl)>> ResolveAllForUserAsync(string userId, CancellationToken ct = default);
}
