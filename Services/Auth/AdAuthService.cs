using System.DirectoryServices.AccountManagement;
using Auth.Web.Configuration;
using Auth.Web.Services.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Auth.Web.Services.Auth;

public class AdAuthService : IAdAuthService
{
    private readonly AdOptions _options;
    private readonly ILogger<AdAuthService> _logger;

    public AdAuthService(IOptions<AdOptions> options, ILogger<AdAuthService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<bool> ValidateAsync(string userName, string password)
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

    private PrincipalContext CreateContext()
    {
        if (!string.IsNullOrWhiteSpace(_options.UserName) && !string.IsNullOrWhiteSpace(_options.Password))
        {
            return new PrincipalContext(ContextType.Domain, _options.Domain, _options.Container, _options.UserName, _options.Password);
        }

        if (!string.IsNullOrWhiteSpace(_options.Container))
        {
            return new PrincipalContext(ContextType.Domain, _options.Domain, _options.Container);
        }

        return new PrincipalContext(ContextType.Domain, _options.Domain);
    }
}
