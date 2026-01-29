using System.DirectoryServices.AccountManagement;
using Auth.Web.Configuration;
using Auth.Web.Services.Abstractions.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Runtime.Versioning;

namespace Auth.Web.Security.Auth;

[SupportedOSPlatform("windows")]
public class AdAuthService : IActiveDirectoryAuthService
{
    private readonly AdOptions _options;
    private readonly ILogger<AdAuthService> _logger;

    public AdAuthService(IOptions<AdOptions> options, ILogger<AdAuthService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<bool> ValidateCredentialsAsync(string userNameOrEmail, string password)
    {
        try
        {
            using var context = CreateContext();

            var domain = _options.Domain ?? string.Empty;
            var isValid = context.ValidateCredentials(userNameOrEmail, password, ContextOptions.Negotiate);

            if (!isValid)
            {
                if (userNameOrEmail.Contains("@", StringComparison.Ordinal))
                {
                    var resolvedUser = TryResolveUserNameByEmail(context, userNameOrEmail);
                    if (!string.IsNullOrWhiteSpace(resolvedUser))
                    {
                        if (!string.IsNullOrWhiteSpace(domain))
                        {
                            isValid = context.ValidateCredentials($"{domain}\\{resolvedUser}", password, ContextOptions.Negotiate);
                        }

                        if (!isValid)
                        {
                            isValid = context.ValidateCredentials(resolvedUser, password, ContextOptions.Negotiate);
                        }
                    }
                }
                else if (!userNameOrEmail.Contains("\\", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(domain))
                {
                    isValid = context.ValidateCredentials($"{domain}\\{userNameOrEmail}", password, ContextOptions.Negotiate);
                }
            }

            return Task.FromResult(isValid);
        }
        catch (PrincipalServerDownException ex)
        {
            _logger.LogError(ex, "Active Directory server is not reachable.");
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate user {User}", userNameOrEmail);
            return Task.FromResult(false);
        }
    }

    public Task<bool> ExistsByEmailAsync(string email)
    {
        try
        {
            using var context = CreateContext();
            using var searcher = new PrincipalSearcher(new UserPrincipal(context)
            {
                EmailAddress = email
            });
            var result = searcher.FindOne();
            return Task.FromResult(result is not null);
        }
        catch (PrincipalServerDownException ex)
        {
            _logger.LogError(ex, "Active Directory server is not reachable.");
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query user by email {Email}", email);
            return Task.FromResult(false);
        }
    }

    public Task<AdUserInfo?> GetUserInfoAsync(string userNameOrEmail)
    {
        try
        {
            using var context = CreateContext();
            using var userPrincipal = UserPrincipal.FindByIdentity(context, userNameOrEmail)
                ?? UserPrincipal.FindByIdentity(context, IdentityType.UserPrincipalName, userNameOrEmail)
                ?? UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, userNameOrEmail);
            if (userPrincipal is null)
            {
                return Task.FromResult<AdUserInfo?>(null);
            }
            var info = new AdUserInfo
            {
                UserName = userPrincipal.SamAccountName ?? userNameOrEmail,
                Email = userPrincipal.EmailAddress,
                DisplayName = userPrincipal.DisplayName
            };
            return Task.FromResult<AdUserInfo?>(info);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetUserInfoAsync failed for {User}", userNameOrEmail);
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

    private static string? TryResolveUserNameByEmail(PrincipalContext context, string email)
    {
        using var searcher = new PrincipalSearcher(new UserPrincipal(context)
        {
            EmailAddress = email
        });
        var result = searcher.FindOne() as UserPrincipal;
        return result?.SamAccountName;
    }
}
