using Auth.Web.Contracts.Auth;
using Auth.Web.Services.Abstractions.Auth.Models;

namespace Auth.Web.Services.Abstractions.Auth;

public interface IAuthFlowService
{
    Task<LoginResult> LoginAsync(LoginRequestDto request);

    /// <summary>
    /// Completes the sign-in for an already-authenticated user by issuing app-specific claims
    /// for the requested <paramref name="clientId"/>. Returns the redirect URL on success, or null
    /// if the user does not have access to the requested application.
    /// </summary>
    Task<string?> SelectAppAsync(string clientId, CancellationToken ct = default);
}
