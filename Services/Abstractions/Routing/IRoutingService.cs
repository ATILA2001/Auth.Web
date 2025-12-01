namespace Auth.Web.Services.Abstractions.Routing;

public interface IRoutingService
{
    Task<(string ClientId, string ReturnUrl)?> ResolveForUserAsync(string userId, CancellationToken ct = default);
}
