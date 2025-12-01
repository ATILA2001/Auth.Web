namespace Auth.Web.Services.Abstractions.Auth;

public interface IActiveDirectoryAuthService
{
    Task<bool> ValidateCredentialsAsync(string userNameOrEmail, string password);
    Task<bool> ExistsByEmailAsync(string email);
    Task<AdUserInfo?> GetUserInfoAsync(string userNameOrEmail);
}

public sealed class AdUserInfo
{
    public string UserName { get; init; } = default!;
    public string? Email { get; init; }
    public string? DisplayName { get; init; }
}
