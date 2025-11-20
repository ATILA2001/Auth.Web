using System.DirectoryServices.AccountManagement;
using Auth.Web.Configuration;
using Auth.Web.Services.Abstractions; // legacy
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AppAD = Auth.Web.Application.Abstractions.IActiveDirectoryAuthService;
using Auth.Web.Application.Abstractions;

namespace Auth.Web.Services.Auth;

public class AdAuthService : IAdAuthService, AppAD
{
    private readonly AdOptions _options;
    private readonly ILogger<AdAuthService> _logger;

    public AdAuthService(IOptions<AdOptions> options, ILogger<AdAuthService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    // Legacy signature
    public Task<bool> ValidateAsync(string userName, string password) => ValidateCredentialsInternal(userName, password);

    // Application signature
    public Task<bool> ValidateCredentialsAsync(string userNameOrEmail, string password) => ValidateCredentialsInternal(userNameOrEmail, password);

    private Task<bool> ValidateCredentialsInternal(string userName, string password)
    {
        try
        {
            using var context = CreateContext();
            var isValid = context.ValidateCredentials(userName, password, ContextOptions.Negotiate);
            return Task.FromResult(isValid);
        }
        catch (PrincipalServerDownException ex)
        {
            _logger.LogError(ex, "Active Directory server is not reachable.");
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate user {User}", userName);
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

    // Application enrichment (simple stub)
    public Task<AdUserInfo?> GetUserInfoAsync(string userNameOrEmail)
    {
        try
        {
            using var context = CreateContext();
            using var userPrincipal = UserPrincipal.FindByIdentity(context, userNameOrEmail);
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

    private PrincipalContext CreateContext() => new(ContextType.Domain, _options.Domain);
}
