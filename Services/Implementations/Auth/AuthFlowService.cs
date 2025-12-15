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

namespace Auth.Web.Services.Implementations.Auth;

public sealed class AuthFlowService : IAuthFlowService
{
    private readonly IActiveDirectoryAuthService _adAuth;
    private readonly IUserManagementService _userManagement;
    private readonly IPermissionService _permissionService;
    private readonly IRoutingService _routingService;
    private readonly IClientService _clientService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUserProvisioningService _userProvisioningService;
    private readonly UserPermissionsAssembler _permissionsAssembler;

    public AuthFlowService(
        IActiveDirectoryAuthService adAuth,
        IUserManagementService userManagement,
        IPermissionService permissionService,
        IRoutingService routingService,
        IClientService clientService,
        IJwtTokenService jwtTokenService,
        IUserProvisioningService userProvisioningService,
        UserPermissionsAssembler permissionsAssembler)
    {
        _adAuth = adAuth;
        _userManagement = userManagement;
        _permissionService = permissionService;
        _routingService = routingService;
        _clientService = clientService;
        _jwtTokenService = jwtTokenService;
        _userProvisioningService = userProvisioningService;
        _permissionsAssembler = permissionsAssembler;
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
            return BuildLoginRedirect("invalid_credentials", "Usuario o contraseña requeridos.", dto.ReturnUrl, dto.ClientId);
        }

        var adOk = await _adAuth.ValidateCredentialsAsync(dto.UserNameOrEmail, dto.Password);
        if (!adOk)
        {
            return BuildLoginRedirect("invalid_credentials", "Usuario o contraseña inválidos.", dto.ReturnUrl, dto.ClientId);
        }

        var user = await _userManagement.FindByNameAsync(dto.UserNameOrEmail)
                   ?? await _userManagement.FindByEmailAsync(dto.UserNameOrEmail)
                   ?? await _userProvisioningService.EnsureUserAsync(dto.UserNameOrEmail);

        var rolesList = await _userManagement.GetRolesAsync(user);
        var roles = rolesList.ToArray();
        var isAdmin = roles.Contains("Admin") || roles.Contains("Administrador");
        if (isAdmin)
        {
            return new LoginResult { SignInAdmin = true, AdminUserId = user.Id, RedirectUrl = "/admin" };
        }

        string clientId;
        string returnUrl;
        ApplicationClient? client = null;

        if (!string.IsNullOrWhiteSpace(dto.ClientId) && !string.IsNullOrWhiteSpace(dto.ReturnUrl))
        {
            client = await _clientService.GetAsync(dto.ClientId);
            if (client is null)
            {
                return BuildLoginRedirect("invalid_client", "Aplicación destino inválida.", dto.ReturnUrl, dto.ClientId);
            }
            if (!_clientService.IsReturnUrlAllowed(client, dto.ReturnUrl))
            {
                return BuildLoginRedirect("invalid_return_url", "URL de retorno inválida.", dto.ReturnUrl, dto.ClientId);
            }
            clientId = dto.ClientId;
            returnUrl = dto.ReturnUrl;
        }
        else
        {
            var routing = await _routingService.ResolveForUserAsync(user.Id);
            if (routing is null)
            {
                return BuildLoginRedirect("no_route", "No se encontró una aplicación de destino para el usuario.", dto.ReturnUrl, dto.ClientId);
            }
            clientId = routing.Value.ClientId;
            returnUrl = routing.Value.ReturnUrl;
            client = await _clientService.GetAsync(clientId);
            if (client is null || !_clientService.IsReturnUrlAllowed(client, returnUrl))
            {
                return BuildLoginRedirect("invalid_return_url", "Aplicación o URL de retorno inválida.", dto.ReturnUrl, dto.ClientId);
            }
        }

        var rawPermissions = await _permissionService.GetAsync(user.UserName!);
        var apps = new List<string> { clientId };
        var claimsModel = _permissionsAssembler.BuildClaims(user, roles, rawPermissions, apps);
        var token = _jwtTokenService.CreateToken(claimsModel, client!.Audience);
        var redirect = QueryHelpers.AddQueryString(returnUrl, "token", token);

        return new LoginResult { RedirectUrl = redirect, SignInAdmin = false };
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
}
