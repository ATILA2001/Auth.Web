namespace Auth.Web.Services.Abstractions.Auth;

public interface IActiveDirectoryAuthService
{
    Task<AdAuthenticationResult> AuthenticateAsync(string userNameOrEmail, string password);
    Task<bool> ExistsByEmailAsync(string email);
    Task<AdUserInfo?> GetUserInfoAsync(string userNameOrEmail);
}

public enum AdAuthenticationStatus
{
    Success,
    InvalidCredentials,
    LockedOut,
    ServerUnavailable,
    LookupFailed,
    UnexpectedError
}

public sealed record AdAuthenticationResult(
    AdAuthenticationStatus Status,
    AdUserInfo? UserInfo = null,
    DateTimeOffset? UnlockAtUtc = null,
    bool RequiresAdministratorUnlock = false,
    int? LockoutThreshold = null,
    string? DiagnosticCode = null)
{
    public bool Succeeded => Status == AdAuthenticationStatus.Success;

    public static AdAuthenticationResult Success(AdUserInfo userInfo)
        => new(AdAuthenticationStatus.Success, UserInfo: userInfo);

    public static AdAuthenticationResult InvalidCredentials()
        => new(
            AdAuthenticationStatus.InvalidCredentials,
            DiagnosticCode: "auth.ad.invalid_credentials");

    public static AdAuthenticationResult LockedOut(
        DateTimeOffset? unlockAtUtc,
        bool requiresAdministratorUnlock,
        int? lockoutThreshold)
        => new(
            AdAuthenticationStatus.LockedOut,
            UnlockAtUtc: unlockAtUtc,
            RequiresAdministratorUnlock: requiresAdministratorUnlock,
            LockoutThreshold: lockoutThreshold,
            DiagnosticCode: "auth.ad.locked_out");

    public static AdAuthenticationResult ServerUnavailable()
        => new(
            AdAuthenticationStatus.ServerUnavailable,
            DiagnosticCode: "auth.ad.unreachable");

    public static AdAuthenticationResult LookupFailed()
        => new(
            AdAuthenticationStatus.LookupFailed,
            DiagnosticCode: "auth.ad.lookup_failed");

    public static AdAuthenticationResult UnexpectedError()
        => new(
            AdAuthenticationStatus.UnexpectedError,
            DiagnosticCode: "auth.ad.unexpected_error");
}

public sealed class AdUserInfo
{
    public string UserName { get; init; } = default!;
    public string? Email { get; init; }
    public string? DisplayName { get; init; }
    public string? EmployeeId { get; init; }
}
