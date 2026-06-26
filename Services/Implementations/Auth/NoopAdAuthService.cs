using Auth.Web.Services.Abstractions.Auth;

namespace Auth.Web.Services.Implementations.Auth;

/// <summary>
/// Noop implementation for platforms where Windows Active Directory APIs are unavailable.
/// </summary>
public sealed class NoopAdAuthService : IActiveDirectoryAuthService
{
    public Task<AdAuthenticationResult> AuthenticateAsync(string userNameOrEmail, string password)
        => Task.FromResult(AdAuthenticationResult.ServerUnavailable());

    public Task<bool> ExistsByEmailAsync(string email)
        => Task.FromResult(false);

    public Task<AdUserInfo?> GetUserInfoAsync(string userNameOrEmail)
        => Task.FromResult<AdUserInfo?>(null);
}
