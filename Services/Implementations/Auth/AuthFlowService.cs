using AuthClaimsModel = Auth.Web.Application.Auth.AuthClaimsModel;
using Auth.Web.Application.Dtos;
using Auth.Web.Application.Permissions;
using Auth.Web.Data.Entities;
using Auth.Web.Services.Abstractions.Auth;
using Auth.Web.Services.Abstractions.Clients;
using Auth.Web.Services.Abstractions.Routing;
using Auth.Web.Services.Abstractions.Users;
using Auth.Web.Services.Abstractions.Permissions;
using Microsoft.AspNetCore.WebUtilities;

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

    public async Task<LoginOutcome> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.UserNameOrEmail) || string.IsNullOrWhiteSpace(request.Password))
        {
            return LoginOutcome.Failure("Usuario o contraseña requeridos.");
        }

        var adOk = await _adAuth.ValidateCredentialsAsync(request.UserNameOrEmail, request.Password);
        if (!adOk)
        {
            return LoginOutcome.Failure("Usuario o contraseña inválidos.");
        }

        var user = await _userManagement.FindByNameAsync(request.UserNameOrEmail)
                   ?? await _userManagement.FindByEmailAsync(request.UserNameOrEmail)
                   ?? await _userProvisioningService.EnsureUserAsync(request.UserNameOrEmail, cancellationToken);

        var rolesList = await _userManagement.GetRolesAsync(user);
        var roles = rolesList.ToArray();
        var isAdmin = roles.Contains("Admin") || roles.Contains("Administrador");
        if (isAdmin)
        {
            return LoginOutcome.Admin("/admin");
        }

        string clientId;
        string returnUrl;
        ApplicationClient? client;

        if (!string.IsNullOrWhiteSpace(request.ClientId) && !string.IsNullOrWhiteSpace(request.ReturnUrl))
        {
            client = await _clientService.GetAsync(request.ClientId);
            if (client is null)
            {
                return LoginOutcome.Failure("Aplicación destino inválida.");
            }
            if (!_clientService.IsReturnUrlAllowed(client, request.ReturnUrl))
            {
                return LoginOutcome.Failure("URL de retorno inválida.");
            }
            clientId = request.ClientId;
            returnUrl = request.ReturnUrl;
        }
        else
        {
            var routing = await _routingService.ResolveForUserAsync(user.Id, cancellationToken);
            if (routing is null)
            {
                return LoginOutcome.Failure("No se encontró una aplicación de destino para el usuario.");
            }
            clientId = routing.Value.ClientId;
            returnUrl = routing.Value.ReturnUrl;
            client = await _clientService.GetAsync(clientId);
            if (client is null || !_clientService.IsReturnUrlAllowed(client, returnUrl))
            {
                return LoginOutcome.Failure("Aplicación o URL de retorno inválida.");
            }
        }

        var rawPermissions = await _permissionService.GetAsync(user.UserName!);
        var apps = new List<string> { clientId };
        var claimsModel = _permissionsAssembler.BuildClaims(user, roles, rawPermissions, apps);
        var token = _jwtTokenService.CreateToken(claimsModel, client!.Audience);
        var redirect = QueryHelpers.AddQueryString(returnUrl, "token", token);

        return LoginOutcome.ExternalApp(redirect);
    }
}
