using Auth.Web.Services.Abstractions.Auth;
using Auth.Web.Contracts.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Auth.Web.Data.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Auth.Web.Services.Abstractions.Clients;
using Auth.Web.Services.Abstractions.Permissions;
using Auth.Web.Application.Permissions;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace Auth.Web.Controllers;

[ApiController]
[Route("connect")]
public class ConnectController : ControllerBase
{
    private readonly IAuthFlowService _authFlowService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IClientService _clientService;
    private readonly IPermissionService _permissionService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly UserPermissionsAssembler _permissionsAssembler;
    private readonly ILogger<ConnectController> _logger;

    public ConnectController(
        IAuthFlowService authFlowService,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IClientService clientService,
        IPermissionService permissionService,
        IJwtTokenService jwtTokenService,
        UserPermissionsAssembler permissionsAssembler,
        ILogger<ConnectController> logger)
    {
        _authFlowService = authFlowService;
        _userManager = userManager;
        _signInManager = signInManager;
        _clientService = clientService;
        _permissionService = permissionService;
        _jwtTokenService = jwtTokenService;
        _permissionsAssembler = permissionsAssembler;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromForm] LoginRequestDto dto)
    {
        var result = await _authFlowService.LoginAsync(dto);

        if (result.SignInAdmin)
        {
            var user = await _userManager.FindByIdAsync(result.AdminUserId!);
            if (user != null)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
            }
        }

        return Redirect(result.RedirectUrl);
    }

    [HttpPost("portal-login")]
    public Task<IActionResult> PortalLogin([FromForm] LoginRequestDto dto)
        => Login(dto);

    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        return Redirect("/Account/Login");
    }

    [HttpGet("launch")]
    [Authorize(Roles = "Admin")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Launch([FromQuery] string? clientId, [FromQuery] string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(returnUrl))
        {
            _logger.LogWarning("Launch: missing_params clientId={ClientId} returnUrl={ReturnUrl}", clientId, returnUrl);
            var url = QueryHelpers.AddQueryString("/admin", new Dictionary<string, string?>
            {
                ["errorCode"] = "missing_params",
                ["error"] = "Parámetros requeridos: clientId y returnUrl"
            });
            return Redirect(url);
        }

        // Require absolute returnUrl (allow http/https)
        if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out var _))
        {
            _logger.LogWarning("Launch: invalid_return_url (not absolute) clientId={ClientId} returnUrl={ReturnUrl}", clientId, returnUrl);
            var url = QueryHelpers.AddQueryString("/admin", new Dictionary<string, string?>
            {
                ["errorCode"] = "invalid_return_url",
                ["error"] = "URL de retorno debe ser absoluta (http/https)"
            });
            return Redirect(url);
        }

        var client = await _clientService.GetAsync(clientId);
        if (client is null)
        {
            _logger.LogWarning("Launch: invalid_client clientId={ClientId} returnUrl={ReturnUrl}", clientId, returnUrl);
            var url = QueryHelpers.AddQueryString("/admin", new Dictionary<string, string?>
            {
                ["errorCode"] = "invalid_client",
                ["error"] = "Aplicación destino inválida"
            });
            return Redirect(url);
        }

        if (!_clientService.IsReturnUrlAllowed(client, returnUrl))
        {
            _logger.LogWarning("Launch: invalid_return_url (not allowed) clientId={ClientId} returnUrl={ReturnUrl}", clientId, returnUrl);
            var url = QueryHelpers.AddQueryString("/admin", new Dictionary<string, string?>
            {
                ["errorCode"] = "invalid_return_url",
                ["error"] = "URL de retorno inválida"
            });
            return Redirect(url);
        }

        // Resolve current signed-in user (prefer id claims -> sub -> name)
        ApplicationUser? user = null;
        var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User?.FindFirst("sub")?.Value;
        if (!string.IsNullOrWhiteSpace(userIdClaim))
        {
            user = await _userManager.FindByIdAsync(userIdClaim);
        }
        if (user is null)
        {
            var name = User?.Identity?.Name;
            if (!string.IsNullOrWhiteSpace(name))
            {
                user = await _userManager.FindByNameAsync(name) ?? await _userManager.FindByEmailAsync(name);
            }
        }
        if (user is null)
        {
            _logger.LogWarning("Launch: user_not_found clientId={ClientId} returnUrl={ReturnUrl}", clientId, returnUrl);
            var url = QueryHelpers.AddQueryString("/admin", new Dictionary<string, string?>
            {
                ["errorCode"] = "user_not_found",
                ["error"] = "No se pudo resolver el usuario actual"
            });
            return Redirect(url);
        }

        var rolesList = await _userManager.GetRolesAsync(user);
        var roles = rolesList.ToArray();
        var permissions = await _permissionService.GetAsync(user.UserName!);
        var apps = new List<string> { clientId };
        var claimsModel = _permissionsAssembler.BuildClaims(user, roles, permissions, apps);
        var token = _jwtTokenService.CreateToken(claimsModel, client.Audience);

        var redirect = QueryHelpers.AddQueryString(returnUrl, "token", token);
        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";
        return Redirect(redirect);
    }
}
