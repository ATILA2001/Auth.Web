using System.Text.Json;
using Auth.Web.Data.Entities;
using Auth.Web.Services.Abstractions.Clients;
using Auth.Web.Repositories.Abstractions.Clients;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Auth.Web.Services.Implementations.Clients;

public class ClientService : IClientService
{
    private readonly IClientRepository _repository;
    private readonly IWebHostEnvironment? _environment;
    private readonly IConfiguration? _configuration;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly ILogger<ClientService>? _logger;

    public ClientService(
        IClientRepository repository,
        IWebHostEnvironment? environment = null,
        IConfiguration? configuration = null,
        IHttpContextAccessor? httpContextAccessor = null,
        ILogger<ClientService>? logger = null)
    {
        _repository = repository;
        _environment = environment;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public Task<ApplicationClient?> GetAsync(string clientId)
    {
        return _repository.GetAsync(clientId);
    }

    public Task<IReadOnlyList<ApplicationClient>> GetAllAsync(CancellationToken ct = default)
    {
        return _repository.GetAllAsync(ct);
    }

    public bool IsReturnUrlAllowed(ApplicationClient client, string returnUrl)
    {
        if (IsLocalAuthWebRequest() && IsLocalhostUrl(returnUrl))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(client.AllowedReturnUrlsJson))
        {
            return false;
        }

        try
        {
            var urls = JsonSerializer.Deserialize<List<string>>(client.AllowedReturnUrlsJson) ?? [];
            return urls.Any(u => IsSameOrigin(u, returnUrl));
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool IsSameOrigin(string? allowedUrl, string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(allowedUrl) || string.IsNullOrWhiteSpace(returnUrl))
            return false;
        if (!Uri.TryCreate(allowedUrl, UriKind.Absolute, out var allowed))
            return false;
        if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out var ret))
            return false;
        return string.Equals(
            allowed.GetLeftPart(UriPartial.Authority),
            ret.GetLeftPart(UriPartial.Authority),
            StringComparison.OrdinalIgnoreCase);
    }

    public string? GetDefaultReturnUrl(ApplicationClient client)
        => GetLandingUrl(client);

    public string? GetLandingUrl(ApplicationClient client, bool useClientDefaultLandingPage = true)
    {
        if (string.IsNullOrWhiteSpace(client.AllowedReturnUrlsJson))
            return null;

        try
        {
            var urls = JsonSerializer.Deserialize<List<string>>(client.AllowedReturnUrlsJson) ?? [];
            var baseUrl = urls.FirstOrDefault(u => !string.IsNullOrWhiteSpace(u));
            if (string.IsNullOrWhiteSpace(baseUrl))
                return null;

            // Area routes pass useClientDefaultLandingPage=false so apps can
            // resolve their own first permitted page when no client fallback is needed.
            var landingPath = useClientDefaultLandingPage && !string.IsNullOrWhiteSpace(client.DefaultLandingPage) ? client.DefaultLandingPage
                            : null;

            var url = landingPath is null
                ? baseUrl
                : AppendPath(baseUrl, landingPath);

            return ResolveReturnUrlForCurrentEnvironment(client, url);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string AppendPath(string baseUrl, string path)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
            return baseUrl;

        var builder = new UriBuilder(uri)
        {
            Path = "/" + path.TrimStart('/')
        };
        return builder.Uri.ToString();
    }

    public string ResolveReturnUrlForCurrentEnvironment(ApplicationClient client, string returnUrl, string? appId = null)
    {
        if (!IsLocalAuthWebRequest())
        {
            return returnUrl;
        }

        if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out var requestedUri))
        {
            return returnUrl;
        }

        if (IsLocalhostUrl(returnUrl))
        {
            return returnUrl;
        }

        var configuredUrl = GetConfiguredDevelopmentRedirect(appId, client.ClientId, client.Audience);
        if (!IsLocalhostUrl(configuredUrl) || !Uri.TryCreate(configuredUrl, UriKind.Absolute, out var localBaseUri))
        {
            _logger?.LogWarning(
                "Development redirect sin mapping local: app='{App}' clientId='{ClientId}' audience='{Audience}' returnUrl='{ReturnUrl}' requestHost='{RequestHost}' environment='{Environment}'.",
                appId,
                client.ClientId,
                client.Audience,
                returnUrl,
                GetRequestHost(),
                _environment?.EnvironmentName);
            return returnUrl;
        }

        var builder = new UriBuilder(localBaseUri)
        {
            Path = requestedUri.AbsolutePath,
            Query = requestedUri.Query,
            Fragment = requestedUri.Fragment
        };

        var rewrittenUrl = builder.Uri.ToString();
        _logger?.LogInformation(
            "Development redirect rewrite: app='{App}' clientId='{ClientId}' audience='{Audience}' from='{OriginalUrl}' to='{RewrittenUrl}' requestHost='{RequestHost}' environment='{Environment}'.",
            appId,
            client.ClientId,
            client.Audience,
            returnUrl,
            rewrittenUrl,
            GetRequestHost(),
            _environment?.EnvironmentName);

        return rewrittenUrl;
    }

    private string? GetConfiguredDevelopmentRedirect(params string?[] appIds)
    {
        foreach (var appId in appIds)
        {
            var configuredUrl = GetConfiguredDevelopmentRedirect(appId);
            if (!string.IsNullOrWhiteSpace(configuredUrl))
            {
                return configuredUrl;
            }
        }

        return null;
    }

    private string? GetConfiguredDevelopmentRedirect(string? appId)
    {
        if (string.IsNullOrWhiteSpace(appId))
        {
            return null;
        }

        var normalizedAppId = appId.Trim();
        var directUrl = _configuration?[$"DevelopmentRedirects:{normalizedAppId}"];
        if (IsLocalhostUrl(directUrl))
        {
            return directUrl;
        }

        return null;
    }

    private bool IsLocalAuthWebRequest()
    {
        var host = GetRequestHost();
        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase);
    }

    private string? GetRequestHost()
    {
        return _httpContextAccessor?.HttpContext?.Request.Host.Host;
    }

    private static bool IsLocalhostUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            && (uri.IsLoopback || string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase));
    }
}
