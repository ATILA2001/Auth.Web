namespace Auth.Web.Application.Abstractions;

public interface IRoutingService
{
    // Resuelve la app y ReturnUrl para un usuario no admin.
    Task<(string ClientId, string ReturnUrl)?> ResolveForUserAsync(string userId, CancellationToken ct = default);
}
