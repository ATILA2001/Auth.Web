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
    private readonly IAdminSignInService _adminSignInService;

    public AuthFlowService(
        IActiveDirectoryAuthService adAuth,
        IUserManagementService userManagement,
        IPermissionService permissionService,
        IRoutingService routingService,
        IClientService clientService,
        IJwtTokenService jwtTokenService,
        IUserProvisioningService userProvisioningService,
        UserPermissionsAssembler permissionsAssembler,
        IAdminSignInService adminSignInService)
    {
        _adAuth = adAuth;
        _userManagement = userManagement;
        _permissionService = permissionService;
        _routingService = routingService;
        _clientService = clientService;
        _jwtTokenService = jwtTokenService;
        _userProvisioningService = userProvisioningService;
        _permissionsAssembler = permissionsAssembler;
        _adminSignInService = adminSignInService;
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
            return await FinalizeResultAsync(BuildLoginRedirect("invalid_credentials", "Usuario o contrase\u00f1a requeridos.", dto.ReturnUrl, dto.ClientId));
        }

        var adOk = await _adAuth.ValidateCredentialsAsync(dto.UserNameOrEmail, dto.Password);
        if (!adOk)
        {
            return await FinalizeResultAsync(BuildLoginRedirect("invalid_credentials", "Usuario o contrase\u00f1a inv\u00e1lidos.", dto.ReturnUrl, dto.ClientId));
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
                return await FinalizeResultAsync(BuildLoginRedirect("invalid_client", "Aplicaci\u00f3n destino inv\u00e1lida.", dto.ReturnUrl, dto.ClientId));
            }
            if (!_clientService.IsReturnUrlAllowed(client, dto.ReturnUrl))
            {
                return await FinalizeResultAsync(BuildLoginRedirect("invalid_return_url", "URL de retorno inv\u00e1lida.", dto.ReturnUrl, dto.ClientId));
            }
            clientId = dto.ClientId;
            returnUrl = dto.ReturnUrl;
        }
        else
        {
            var routing = await _routingService.ResolveForUserAsync(user.Id);
            if (routing is null)
            {
                return await FinalizeResultAsync(BuildLoginRedirect("no_route", "No se encontr\u00f3 una aplicaci\u00f3n de destino para el usuario.", dto.ReturnUrl, dto.ClientId));
            }
            clientId = routing.Value.ClientId;
            returnUrl = routing.Value.ReturnUrl;
            client = await _clientService.GetAsync(clientId);
            if (client is null || !_clientService.IsReturnUrlAllowed(client, returnUrl))
            {
                return await FinalizeResultAsync(BuildLoginRedirect("invalid_return_url", "Aplicaci\u00f3n o URL de retorno inv\u00e1lida.", dto.ReturnUrl, dto.ClientId));
            }
        }

        var rawPermissions = await _permissionService.GetAsync(user.UserName!);
        var apps = new List<string> { clientId };
        var claimsModel = _permissionsAssembler.BuildClaims(user, roles, rawPermissions, apps);
        var token = _jwtTokenService.CreateToken(claimsModel, client!.Audience);
        var redirect = QueryHelpers.AddQueryString(returnUrl, "token", token);

        return await FinalizeResultAsync(new LoginResult { RedirectUrl = redirect, SignInAdmin = false });
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
}
