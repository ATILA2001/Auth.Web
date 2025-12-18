namespace Auth.Web.Services.Implementations.Auth;

/// <summary>
/// Noop implementation of IActiveDirectoryAuthService for non-Windows platforms.
/// Always returns false for validation and null for user info.
/// </summary>
public sealed class NoopAdAuthService : Abstractions.Auth.IActiveDirectoryAuthService
{
    public Task<bool> ValidateCredentialsAsync(string userNameOrEmail, string password)
        => Task.FromResult(false);

    public Task<bool> ExistsByEmailAsync(string email)
        => Task.FromResult(false);

    public Task<Abstractions.Auth.AdUserInfo?> GetUserInfoAsync(string userNameOrEmail)
        => Task.FromResult<Abstractions.Auth.AdUserInfo?>(null);
}
