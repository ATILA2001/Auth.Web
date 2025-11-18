using System.Net;
using System.Net.Http.Json;
using Auth.Web.Configuration;
using Auth.Web.Services.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Auth.Web.Domain.Entities;
using System.Linq;

namespace Auth.Web.Services.Auth;

public sealed class LoginService : ILoginService
{
    private readonly IAdAuthService _ad;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRoutingService _routing;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LoginService> _logger;
    private readonly FeatureOptions _features;

    public LoginService(
        IAdAuthService ad,
        UserManager<ApplicationUser> userManager,
        IRoutingService routing,
        IHttpClientFactory httpClientFactory,
        IOptions<FeatureOptions> features,
        ILogger<LoginService> logger)
    {
        _ad = ad;
        _userManager = userManager;
        _routing = routing;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _features = features.Value;
    }

    public async Task<LoginResult> LoginAsync(string userNameOrEmail, string password, CancellationToken ct = default)
    {
        // 1) AD validation
        var ok = await _ad.ValidateAsync(userNameOrEmail, password);
        if (!ok)
        {
            return new LoginResult(false, "Error: Invalid login attempt (AD).", null);
        }

        // 2) Find local user
        var user = await _userManager.FindByNameAsync(userNameOrEmail) ?? await _userManager.FindByEmailAsync(userNameOrEmail);
        if (user is null)
        {
            return new LoginResult(false, "Error: No local account found. Please register first.", null);
        }

        // Check admin role: SIN cookie, solo redirección
        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains("Admin"))
        {
            // No SignInAsync (cookies deshabilitadas)
            return new LoginResult(true, null, "/admin");
        }

        // 3) Resolve area-based route (non-admin)
        var target = await _routing.ResolveForUserAsync(user.Id, ct);
        if (target is null)
        {
            _logger.LogWarning("Routing no resolvió para usuario {UserId}", user.Id);
            return new LoginResult(false, "No hay una regla de redirección para tu usuario/áreas.", null);
        }

        // 4) Request token and redirect (optional)
        if (!_features.EnableTokenIssuance)
        {
            var redirectNoToken = target.Value.ReturnUrl; // direct navigation
            return new LoginResult(true, null, redirectNoToken);
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsJsonAsync("/connect/login", new
            {
                UserName = userNameOrEmail,
                Password = password,
                ClientId = target.Value.ClientId,
                ReturnUrl = target.Value.ReturnUrl,
                DisplayName = (string?)null
            }, ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                if (string.IsNullOrWhiteSpace(error))
                {
                    error = $"Login error ({(int)response.StatusCode}).";
                }
                return new LoginResult(false, error, null);
            }

            var result = await response.Content.ReadFromJsonAsync<LoginResultDto>(cancellationToken: ct);
            if (result is null || string.IsNullOrWhiteSpace(result.Redirect))
            {
                return new LoginResult(false, "Respuesta inválida del servidor.", null);
            }

            return new LoginResult(true, null, result.Redirect);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login communication error");
            return new LoginResult(false, $"Error de comunicación: {ex.Message}", null);
        }
    }

    private sealed class LoginResultDto
    {
        public string? Redirect { get; set; }
    }
}
