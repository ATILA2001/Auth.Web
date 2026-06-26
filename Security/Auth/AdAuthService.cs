using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Auth.Web.Configuration;
using Auth.Web.Services.Abstractions.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Auth.Web.Security.Auth;

[SupportedOSPlatform("windows")]
public class AdAuthService : IActiveDirectoryAuthService
{
    private const int UserAccountControlLockoutFlag = 0x0010;

    private readonly AdOptions _options;
    private readonly ILogger<AdAuthService> _logger;

    public AdAuthService(IOptions<AdOptions> options, ILogger<AdAuthService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<AdAuthenticationResult> AuthenticateAsync(string userNameOrEmail, string password)
    {
        try
        {
            using var context = CreateContext();
            using var userPrincipal = FindUser(context, userNameOrEmail);
            if (userPrincipal is null)
            {
                _logger.LogWarning("Active Directory user lookup failed for {User}.", userNameOrEmail);
                return Task.FromResult(AdAuthenticationResult.InvalidCredentials());
            }

            var credentialIdentity = GetCredentialIdentity(userPrincipal, userNameOrEmail);
            var lockout = GetLockoutState(userPrincipal);
            if (lockout.IsLockedOut)
            {
                return Task.FromResult(ToLockedOutResult(lockout));
            }

            var isValid = ValidateCredentialsOnce(context, credentialIdentity, password);

            if (isValid)
            {
                var userInfo = BuildUserInfo(userPrincipal, userNameOrEmail);
                return Task.FromResult(AdAuthenticationResult.Success(userInfo));
            }

            // The failed validation may be the attempt that reached the AD threshold.
            // Refresh the account state without submitting the password a second time.
            using var refreshedPrincipal = FindUser(context, credentialIdentity);
            if (refreshedPrincipal is not null)
            {
                var refreshedLockout = GetLockoutState(refreshedPrincipal);
                if (refreshedLockout.IsLockedOut)
                {
                    return Task.FromResult(ToLockedOutResult(refreshedLockout));
                }
            }

            return Task.FromResult(AdAuthenticationResult.InvalidCredentials());
        }
        catch (PrincipalServerDownException ex)
        {
            _logger.LogError(ex, "Active Directory server is not reachable.");
            return Task.FromResult(AdAuthenticationResult.ServerUnavailable());
        }
        catch (PrincipalOperationException ex)
        {
            _logger.LogError(ex, "Active Directory lookup failed for {User}.", userNameOrEmail);
            return Task.FromResult(AdAuthenticationResult.LookupFailed());
        }
        catch (DirectoryServicesCOMException ex)
        {
            _logger.LogError(ex, "Active Directory directory operation failed for {User}.", userNameOrEmail);
            return Task.FromResult(AdAuthenticationResult.LookupFailed());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected Active Directory authentication failure for {User}.", userNameOrEmail);
            return Task.FromResult(AdAuthenticationResult.UnexpectedError());
        }
    }

    public Task<bool> ExistsByEmailAsync(string email)
    {
        try
        {
            using var context = CreateContext();
            using var user = FindUserByEmail(context, email);
            return Task.FromResult(user is not null);
        }
        catch (PrincipalServerDownException ex)
        {
            _logger.LogError(ex, "Active Directory server is not reachable.");
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query user by email {Email}.", email);
            return Task.FromResult(false);
        }
    }

    public Task<AdUserInfo?> GetUserInfoAsync(string userNameOrEmail)
    {
        try
        {
            using var context = CreateContext();
            using var userPrincipal = FindUser(context, userNameOrEmail);
            return Task.FromResult(userPrincipal is null
                ? null
                : BuildUserInfo(userPrincipal, userNameOrEmail));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetUserInfoAsync failed for {User}.", userNameOrEmail);
            return Task.FromResult<AdUserInfo?>(null);
        }
    }

    private PrincipalContext CreateContext()
    {
        var domain = _options.Domain;
        var container = string.IsNullOrWhiteSpace(_options.Container) ? null : _options.Container;
        var userName = string.IsNullOrWhiteSpace(_options.UserName) ? null : _options.UserName;
        var password = string.IsNullOrWhiteSpace(_options.Password) ? null : _options.Password;

        if (!string.IsNullOrWhiteSpace(userName) && !string.IsNullOrWhiteSpace(password))
        {
            return new PrincipalContext(ContextType.Domain, domain, container, userName, password);
        }

        return string.IsNullOrWhiteSpace(container)
            ? new PrincipalContext(ContextType.Domain, domain)
            : new PrincipalContext(ContextType.Domain, domain, container);
    }

    private static UserPrincipal? FindUser(PrincipalContext context, string identity)
    {
        var user = UserPrincipal.FindByIdentity(context, identity)
            ?? UserPrincipal.FindByIdentity(context, IdentityType.UserPrincipalName, identity)
            ?? UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, identity);

        if (user is not null || !identity.Contains('@', StringComparison.Ordinal))
        {
            return user;
        }

        return FindUserByEmail(context, identity);
    }

    private static UserPrincipal? FindUserByEmail(PrincipalContext context, string email)
    {
        if (!IsValidEmailForLdap(email))
        {
            return null;
        }

        using var searcher = new PrincipalSearcher(new UserPrincipal(context)
        {
            EmailAddress = email
        });
        return searcher.FindOne() as UserPrincipal;
    }

    private static string GetCredentialIdentity(UserPrincipal userPrincipal, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(fallback) && fallback.Contains('\\', StringComparison.Ordinal))
        {
            return fallback;
        }

        if (!string.IsNullOrWhiteSpace(userPrincipal.SamAccountName))
        {
            return userPrincipal.SamAccountName;
        }

        if (!string.IsNullOrWhiteSpace(userPrincipal.UserPrincipalName))
        {
            return userPrincipal.UserPrincipalName;
        }

        return fallback;
    }

    private static AdUserInfo BuildUserInfo(UserPrincipal userPrincipal, string fallbackUserName)
    {
        string? employeeId = null;
        if (userPrincipal.GetUnderlyingObject() is DirectoryEntry directoryEntry)
        {
            employeeId = directoryEntry.Properties["employeeID"]?.Value?.ToString()
                ?? directoryEntry.Properties["employeeid"]?.Value?.ToString();
        }

        return new AdUserInfo
        {
            UserName = userPrincipal.SamAccountName ?? fallbackUserName,
            Email = userPrincipal.EmailAddress,
            DisplayName = userPrincipal.DisplayName,
            EmployeeId = employeeId
        };
    }

    private AdAccountLockoutState GetLockoutState(UserPrincipal userPrincipal)
    {
        try
        {
            var directoryEntry = (DirectoryEntry)userPrincipal.GetUnderlyingObject();
            RefreshAccountProperties(directoryEntry);

            var computedFlags = ReadInt32(directoryEntry, "msDS-User-Account-Control-Computed");
            bool isLockedOut;
            if (computedFlags.HasValue)
            {
                isLockedOut = (computedFlags.Value & UserAccountControlLockoutFlag) != 0;
            }
            else
            {
                try
                {
                    isLockedOut = userPrincipal.IsAccountLockedOut();
                }
                catch (PrincipalOperationException ex)
                {
                    _logger.LogWarning(ex, "Could not read computed lockout state for AD user {User}.", userPrincipal.SamAccountName);
                    return AdAccountLockoutState.NotLocked;
                }
            }

            if (!isLockedOut)
            {
                return AdAccountLockoutState.NotLocked;
            }

            var lockoutAtUtc = ToFileTimeUtc(ReadInt64(directoryEntry, "lockoutTime"));
            var policy = ReadEffectiveLockoutPolicy(directoryEntry);
            var requiresAdministratorUnlock = policy.Duration == TimeSpan.Zero;
            DateTimeOffset? unlockAtUtc = !requiresAdministratorUnlock
                && lockoutAtUtc.HasValue
                && policy.Duration.HasValue
                    ? lockoutAtUtc.Value + policy.Duration.Value
                    : null;

            return new AdAccountLockoutState(
                true,
                unlockAtUtc,
                requiresAdministratorUnlock,
                policy.Threshold);
        }
        catch (Exception ex) when (ex is COMException or UnauthorizedAccessException or PrincipalOperationException or InvalidOperationException or NotSupportedException or InvalidCastException or ObjectDisposedException)
        {
            _logger.LogWarning(ex, "Could not read Active Directory lockout state for user {User}.", userPrincipal.SamAccountName);
            return AdAccountLockoutState.NotLocked;
        }
    }
    private AdLockoutPolicy ReadEffectiveLockoutPolicy(DirectoryEntry userEntry)
    {
        try
        {
            var resultantPso = ReadString(userEntry, "msDS-ResultantPSO");
            using var policyEntry = !string.IsNullOrWhiteSpace(resultantPso)
                ? CreateDirectoryEntry($"LDAP://{resultantPso}")
                : CreateDomainPolicyEntry();

            var thresholdProperty = !string.IsNullOrWhiteSpace(resultantPso)
                ? "msDS-LockoutThreshold"
                : "lockoutThreshold";
            var durationProperty = !string.IsNullOrWhiteSpace(resultantPso)
                ? "msDS-LockoutDuration"
                : "lockoutDuration";

            policyEntry.RefreshCache([thresholdProperty, durationProperty]);
            return new AdLockoutPolicy(
                ReadInt32(policyEntry, thresholdProperty),
                ToDuration(ReadInt64(policyEntry, durationProperty)));
        }
        catch (Exception ex) when (ex is COMException or UnauthorizedAccessException or PrincipalOperationException or InvalidOperationException or NotSupportedException)
        {
            _logger.LogWarning(ex, "Could not read the effective Active Directory lockout policy.");
            return AdLockoutPolicy.Unknown;
        }
    }

    private DirectoryEntry CreateDomainPolicyEntry()
    {
        using var rootDse = CreateDirectoryEntry("LDAP://RootDSE");
        rootDse.RefreshCache(["defaultNamingContext"]);
        var defaultNamingContext = rootDse.Properties["defaultNamingContext"]?.Value?.ToString();
        if (string.IsNullOrWhiteSpace(defaultNamingContext))
        {
            throw new InvalidOperationException("Active Directory did not return defaultNamingContext.");
        }

        return CreateDirectoryEntry($"LDAP://{defaultNamingContext}");
    }

    private DirectoryEntry CreateDirectoryEntry(string path)
    {
        if (!string.IsNullOrWhiteSpace(_options.UserName)
            && !string.IsNullOrWhiteSpace(_options.Password))
        {
            return new DirectoryEntry(
                path,
                _options.UserName,
                _options.Password,
                AuthenticationTypes.Secure);
        }

        return new DirectoryEntry(path, null, null, AuthenticationTypes.Secure);
    }

    private static void RefreshAccountProperties(DirectoryEntry entry)
    {
        try
        {
            entry.RefreshCache([
                "msDS-User-Account-Control-Computed",
                "lockoutTime",
                "msDS-ResultantPSO"
            ]);
        }
        catch (COMException)
        {
            // Individual property reads below still work on older domains or restricted accounts.
            entry.RefreshCache();
        }
    }

    private static string? ReadString(DirectoryEntry entry, string propertyName)
        => entry.Properties[propertyName]?.Value?.ToString();

    private static int? ReadInt32(DirectoryEntry entry, string propertyName)
    {
        var value = entry.Properties[propertyName]?.Value;
        if (value is null)
        {
            return null;
        }

        try
        {
            return Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            return null;
        }
    }

    private static long? ReadInt64(DirectoryEntry entry, string propertyName)
    {
        var value = entry.Properties[propertyName]?.Value;
        if (value is null)
        {
            return null;
        }

        if (value is long longValue)
        {
            return longValue;
        }

        try
        {
            return Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            var type = value.GetType();
            var highPart = type.GetProperty("HighPart")?.GetValue(value);
            var lowPart = type.GetProperty("LowPart")?.GetValue(value);
            if (highPart is null || lowPart is null)
            {
                return null;
            }

            var high = Convert.ToInt32(highPart, System.Globalization.CultureInfo.InvariantCulture);
            var low = Convert.ToUInt32(lowPart, System.Globalization.CultureInfo.InvariantCulture);
            return ((long)high << 32) | low;
        }
    }

    internal static TimeSpan? ToDuration(long? adInterval)
    {
        if (!adInterval.HasValue)
        {
            return null;
        }

        if (adInterval.Value == 0)
        {
            return TimeSpan.Zero;
        }

        if (adInterval.Value == long.MinValue)
        {
            return TimeSpan.MaxValue;
        }

        return TimeSpan.FromTicks(Math.Abs(adInterval.Value));
    }

    internal static DateTimeOffset? ToFileTimeUtc(long? fileTime)
    {
        if (!fileTime.HasValue || fileTime.Value <= 0)
        {
            return null;
        }

        try
        {
            return new DateTimeOffset(DateTime.FromFileTimeUtc(fileTime.Value));
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private bool ValidateCredentialsOnce(PrincipalContext context, string credentialIdentity, string password)
    {
        try
        {
            return context.ValidateCredentials(
                credentialIdentity,
                password,
                ContextOptions.Negotiate);
        }
        catch (DirectoryServicesCOMException ex) when (IsInvalidCredentialException(ex))
        {
            _logger.LogWarning(ex, "Active Directory rejected credentials for {User}.", credentialIdentity);
            return false;
        }
        catch (COMException ex) when (IsInvalidCredentialException(ex))
        {
            _logger.LogWarning(ex, "Active Directory rejected credentials for {User}.", credentialIdentity);
            return false;
        }
    }

    private static bool IsInvalidCredentialException(Exception exception)
    {
        const int logonFailure = unchecked((int)0x8007052E);
        return exception.HResult == logonFailure;
    }

    private static AdAuthenticationResult ToLockedOutResult(AdAccountLockoutState state)
        => AdAuthenticationResult.LockedOut(
            state.UnlockAtUtc,
            state.RequiresAdministratorUnlock,
            state.LockoutThreshold);

    private static bool IsValidEmailForLdap(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > 256)
        {
            return false;
        }

        foreach (var ch in email)
        {
            if (ch is '(' or ')' or '\\' or '*' or '\0')
            {
                return false;
            }
        }

        var atIndex = email.IndexOf('@');
        return atIndex > 0
            && atIndex < email.Length - 1
            && email.IndexOf('@', atIndex + 1) < 0;
    }

    private sealed record AdLockoutPolicy(int? Threshold, TimeSpan? Duration)
    {
        public static AdLockoutPolicy Unknown { get; } = new(null, null);
    }

    private sealed record AdAccountLockoutState(
        bool IsLockedOut,
        DateTimeOffset? UnlockAtUtc,
        bool RequiresAdministratorUnlock,
        int? LockoutThreshold)
    {
        public static AdAccountLockoutState NotLocked { get; } = new(false, null, false, null);
    }
}
