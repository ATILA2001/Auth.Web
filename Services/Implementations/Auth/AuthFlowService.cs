using AuthClaimsModel = Auth.Web.Application.Auth.AuthClaimsModel;
using Auth.Web.Contracts.Auth;
using Auth.Web.Services.Abstractions.Auth;
using Auth.Web.Services.Abstractions.Clients;
using Auth.Web.Services.Abstractions.Routing;
using Auth.Web.Services.Abstractions.Users;
using Auth.Web.Services.Abstractions.Permissions;
using Microsoft.AspNetCore.WebUtilities;
using Auth.Web.Services.Abstractions.Auth.Models;
using Auth.Web.Application.Permissions;
using Auth.Web.Data.Entities;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace Auth.Web.Services.Implementations.Auth;

public sealed class AuthFlowService : IAuthFlowService
{
    private readonly IActiveDirectoryAuthService _adAuth;
    private readonly IUserManagementService _userManagement;
    private readonly IPermissionService _permissionService;
    private readonly IRoutingService _routingService;
    private readonly IClientService _clientService;
    private readonly IUserProvisioningService _userProvisioningService;
    private readonly UserPermissionsAssembler _permissionsAssembler;
    private readonly IAdminSignInService _adminSignInService;
    private readonly IAuthenticationService _authenticationService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthFlowService(
        IActiveDirectoryAuthService adAuth,
        IUserManagementService userManagement,
        IPermissionService permissionService,
        IRoutingService routingService,
        IClientService clientService,
        IUserProvisioningService userProvisioningService,
        UserPermissionsAssembler permissionsAssembler,
        IAdminSignInService adminSignInService,
        IAuthenticationService authenticationService,
        IHttpContextAccessor httpContextAccessor)
    {
        _adAuth = adAuth;
        _userManagement = userManagement;
        _permissionService = permissionService;
        _routingService = routingService;
        _clientService = clientService;
        _userProvisioningService = userProvisioningService;
        _permissionsAssembler = permissionsAssembler;
        _adminSignInService = adminSignInService;
        _authenticationService = authenticationService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<LoginResult> LoginAsync(LoginRequestDto request)
    {
        var dto = new LoginRequestDto
        {
            UserNameOrEmail = request.UserNameOrEmail?.Trim() ?? string.Empty,
            Password = request.Password ?? string.Empty,
            ReturnUrl = string.IsNullOrWhiteSpace(request.ReturnUrl) ? null : request.ReturnUrl.Trim(),
            ClientId = string.IsNullOrWhiteSpace(request.ClientId) ? null : request.ClientId.Trim()
        };

        if (string.IsNullOrWhiteSpace(dto.UserNameOrEmail) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return await FinalizeResultAsync(BuildLoginRedirect("invalid_credentials", "Usuario o contraseña requeridos.", dto.ReturnUrl, dto.ClientId));
        }

        var adOk = await _adAuth.ValidateCredentialsAsync(dto.UserNameOrEmail, dto.Password);
        if (!adOk)
        {
            return await FinalizeResultAsync(BuildLoginRedirect("invalid_credentials", "Usuario o contraseña inválidos.", dto.ReturnUrl, dto.ClientId));
        }

        var user = await _userManagement.FindByNameAsync(dto.UserNameOrEmail)
                   ?? await _userManagement.FindByEmailAsync(dto.UserNameOrEmail)
                   ?? await _userProvisioningService.EnsureUserAsync(dto.UserNameOrEmail);

        var rolesList = await _userManagement.GetRolesAsync(user);
        var roles = rolesList.ToArray();
        var isAdmin = roles.Contains("Admin") || roles.Contains("Administrador");
        if (isAdmin)
        {
            return await FinalizeResultAsync(new LoginResult { SignInAdmin = true, AdminUserId = user.Id, RedirectUrl = "/admin" });
        }

        string clientId;
        string returnUrl;
        ApplicationClient? client = null;

        if (!string.IsNullOrWhiteSpace(dto.ClientId) && !string.IsNullOrWhiteSpace(dto.ReturnUrl))
        {
            client = await _clientService.GetAsync(dto.ClientId);
            if (client is null)
            {
                return await FinalizeResultAsync(BuildLoginRedirect("invalid_client", "Aplicación destino inválida.", dto.ReturnUrl, dto.ClientId));
            }
            if (!_clientService.IsReturnUrlAllowed(client, dto.ReturnUrl))
            {
                return await FinalizeResultAsync(BuildLoginRedirect("invalid_return_url", "URL de retorno inválida.", dto.ReturnUrl, dto.ClientId));
            }
            clientId = dto.ClientId;
            returnUrl = dto.ReturnUrl;
        }
        else
        {
            var routing = await _routingService.ResolveForUserAsync(user.Id);
            if (routing is null)
            {
                return await FinalizeResultAsync(BuildLoginRedirect("no_route", "No se encontró una aplicación de destino para el usuario.", dto.ReturnUrl, dto.ClientId));
            }
            clientId = routing.Value.ClientId;
            returnUrl = routing.Value.ReturnUrl;
            client = await _clientService.GetAsync(clientId);
            if (client is null || !_clientService.IsReturnUrlAllowed(client, returnUrl))
            {
                return await FinalizeResultAsync(BuildLoginRedirect("invalid_return_url", "Aplicación o URL de retorno inválida.", dto.ReturnUrl, dto.ClientId));
            }
        }

        var rawPermissions = await _permissionService.GetAsync(user.UserName!);
        var apps = new List<string> { clientId };
        var claimsModel = _permissionsAssembler.BuildClaims(user, roles, rawPermissions, apps);
        await SignInAsync(claimsModel);

        return await FinalizeResultAsync(new LoginResult { RedirectUrl = returnUrl, SignInAdmin = false });
    }

    private static LoginResult BuildLoginRedirect(string errorCode, string errorMessage, string? returnUrl, string? clientId)
    {
        var query = new Dictionary<string, string?>
        {
            ["errorCode"] = errorCode,
            ["error"] = errorMessage,
            ["returnUrl"] = returnUrl,
            ["clientId"] = clientId
        };
        var url = QueryHelpers.AddQueryString("/Account/Login", query!);
        return new LoginResult { RedirectUrl = url, SignInAdmin = false };
    }

    private async Task<LoginResult> FinalizeResultAsync(LoginResult result)
    {
        if (result.SignInAdmin && !string.IsNullOrWhiteSpace(result.AdminUserId))
        {
            await _adminSignInService.SignInAsync(result.AdminUserId);
        }

        return result;
    }

    private async Task SignInAsync(AuthClaimsModel claimsModel)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            throw new InvalidOperationException("HttpContext no disponible para iniciar sesión.");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, claimsModel.UserId),
            new(ClaimTypes.Name, claimsModel.DisplayName ?? claimsModel.Email ?? claimsModel.UserId)
        };

        if (!string.IsNullOrWhiteSpace(claimsModel.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, claimsModel.Email));
        }

        foreach (var role in claimsModel.Roles.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (var area in claimsModel.Areas.Distinct())
        {
            claims.Add(new Claim("area", area));
        }

        if (!string.IsNullOrWhiteSpace(claimsModel.PermissionsJson))
        {
            claims.Add(new Claim("perms_json", claimsModel.PermissionsJson));
        }

        claims.Add(new Claim("perms_version", claimsModel.PermissionsVersion.ToString()));

        // Add auth_time claim to record session start time (UTC, ISO 8601)
        claims.Add(new Claim("auth_time", DateTime.UtcNow.ToString("o")));

        foreach (var app in claimsModel.Apps)
        {
            claims.Add(new Claim("app", app));
        }

        var identity = new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme);
        var principal = new ClaimsPrincipal(identity);
        var authProps = new AuthenticationProperties
        {
            IsPersistent = false,
            AllowRefresh = true
        };

        await _authenticationService.SignInAsync(httpContext, IdentityConstants.ApplicationScheme, principal, authProps);
    }
}
