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
using Auth.Web.Configuration;
using Microsoft.Extensions.Options;

namespace Auth.Web.Services.Implementations.Auth;

public sealed class AuthFlowService : IAuthFlowService
{
    private readonly IActiveDirectoryAuthService _adAuth;
    private readonly IUserManagementService _userManagement;
    private readonly IPermissionService _permissionService;
    private readonly IRoutingService _routingService;
    private readonly ILogger<AuthFlowService> _logger;
    private readonly IClientService _clientService;
    private readonly IUserProvisioningService _userProvisioningService;
    private readonly UserPermissionsAssembler _permissionsAssembler;
    private readonly IAdminSignInService _adminSignInService;
    private readonly IAuthenticationService _authenticationService;
    private readonly IUserClaimsPrincipalFactory<ApplicationUser> _claimsPrincipalFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TestUsersOptions _testUsers;
    private readonly FeatureOptions _features;

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
        IUserClaimsPrincipalFactory<ApplicationUser> claimsPrincipalFactory,
        IHttpContextAccessor httpContextAccessor,
        IOptions<TestUsersOptions> testUsersOptions,
        IOptions<FeatureOptions> featureOptions,
        ILogger<AuthFlowService> logger)
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
        _logger = logger;
        _claimsPrincipalFactory = claimsPrincipalFactory;
        _httpContextAccessor = httpContextAccessor;
        _testUsers = testUsersOptions.Value ?? new TestUsersOptions();
        _features = featureOptions.Value ?? new FeatureOptions();
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

        _logger.LogInformation("Login intento: user='{User}' clientId='{ClientId}' returnUrl='{ReturnUrl}'",
            dto.UserNameOrEmail, dto.ClientId, dto.ReturnUrl);

        if (string.IsNullOrWhiteSpace(dto.UserNameOrEmail) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return await FinalizeResultAsync(BuildLoginRedirect("invalid_credentials", "Usuario o contraseña requeridos.", dto.ReturnUrl, dto.ClientId));
        }

        var testUser = _features.EnableTestUsers ? TryGetTestUser(dto.UserNameOrEmail) : null;

        // dbAuthUser: user retrieved from DB used for password verification when username ends with .test
        ApplicationUser? dbAuthUser = null;

        if (testUser is not null)
        {
            if (!PasswordMatches(dto.Password, testUser.Password))
            {
                return await FinalizeResultAsync(BuildLoginRedirect("invalid_credentials", "Usuario o contraseña inválidos.", dto.ReturnUrl, dto.ClientId));
            }
        }
        else
        {
            var isDbTest = dto.UserNameOrEmail.EndsWith(".test", StringComparison.OrdinalIgnoreCase);

            if (isDbTest)
            {
                dbAuthUser = await _userManagement.FindByNameAsync(dto.UserNameOrEmail)
                             ?? await _userManagement.FindByEmailAsync(dto.UserNameOrEmail);

                var dbPasswordOk = dbAuthUser is not null && VerifyPasswordHash(dbAuthUser.PasswordHash, dto.Password);

                if (!dbPasswordOk)
                {
                    // Fallback to AD if DB check fails for .test users
                    var adOkFallback = await _adAuth.ValidateCredentialsAsync(dto.UserNameOrEmail, dto.Password);
                    if (!adOkFallback)
                    {
                        return await FinalizeResultAsync(BuildLoginRedirect("invalid_credentials", "Usuario o contraseña inválidos.", dto.ReturnUrl, dto.ClientId));
                    }
                }
            }
            else
            {
                var adOk = await _adAuth.ValidateCredentialsAsync(dto.UserNameOrEmail, dto.Password);
                if (!adOk)
                {
                    return await FinalizeResultAsync(BuildLoginRedirect("invalid_credentials", "Usuario o contraseña inválidos.", dto.ReturnUrl, dto.ClientId));
                }
            }
        }

        var user = dbAuthUser ?? await _userManagement.FindByNameAsync(dto.UserNameOrEmail)
                   ?? await _userManagement.FindByEmailAsync(dto.UserNameOrEmail);

        if (user is null && testUser is not null)
        {
            var testEmail = testUser.Email;
            user = await _userManagement.FindByNameAsync(testEmail)
                   ?? await _userManagement.FindByEmailAsync(testEmail);

            if (user is null)
            {
                user = await _userProvisioningService.EnsureUserAsync(testEmail);
            }
        }

        user ??= await _userProvisioningService.EnsureUserAsync(dto.UserNameOrEmail);

        var rolesList = await _userManagement.GetRolesAsync(user);
        var roles = rolesList.ToArray();
        var effectiveRoles = roles
            .Concat(testUser?.Roles ?? Enumerable.Empty<string>())
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var isAdmin = effectiveRoles.Contains("Admin") || effectiveRoles.Contains("Administrador");
        var hasReturnUrl = !string.IsNullOrWhiteSpace(dto.ReturnUrl);
        var hasClientId = !string.IsNullOrWhiteSpace(dto.ClientId);
        var hasClientRequest = hasClientId && hasReturnUrl;
        int? testAreaId = null;
        if (testUser is not null && !string.IsNullOrWhiteSpace(testUser.Area) && int.TryParse(testUser.Area, out var parsedArea))
        {
            testAreaId = parsedArea;
        }

        if (isAdmin)
        {
            var adminPermissions = await _permissionService.GetAsync(
                user.UserName!,
                roleNamesOverride: effectiveRoles,
                areaIdsOverride: testAreaId.HasValue ? new[] { testAreaId.Value } : null);
            var adminClaims = _permissionsAssembler.BuildClaims(user, effectiveRoles, adminPermissions, Array.Empty<string>());
            adminClaims = MergeTestUserClaims(adminClaims, testUser);
            await SignInAsync(user, adminClaims);

            return await FinalizeResultAsync(new LoginResult { RedirectUrl = "/admin", SignInAdmin = false });
        }

        string clientId;
        string returnUrl;
        ApplicationClient? client = null;

        if (hasClientRequest)
        {
            client = await _clientService.GetAsync(dto.ClientId);
            if (client is null)
            {
                _logger.LogWarning("Login: cliente '{ClientId}' no encontrado en DB.", dto.ClientId);
                return await FinalizeResultAsync(BuildLoginRedirect("invalid_client", "Aplicación destino inválida.", dto.ReturnUrl, dto.ClientId));
            }
            if (!_clientService.IsReturnUrlAllowed(client, dto.ReturnUrl))
            {
                _logger.LogWarning("Login: returnUrl '{ReturnUrl}' no permitida para cliente '{ClientId}'. AllowedUrls={Allowed}",
                    dto.ReturnUrl, dto.ClientId, client.AllowedReturnUrlsJson);
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
                _logger.LogWarning("Login: sin ruta activa para usuario '{UserId}'. hasReturnUrl={HasReturnUrl}, hasClientId={HasClientId}",
                    user.Id, hasReturnUrl, hasClientId);
                return await FinalizeResultAsync(BuildLoginRedirect("no_route", "No tiene permisos para ver esta página.", dto.ReturnUrl, dto.ClientId));
            }
            clientId = routing.Value.ClientId;
            client = await _clientService.GetAsync(clientId);
            if (client is null)
            {
                return await FinalizeResultAsync(BuildLoginRedirect("invalid_return_url", "Aplicación o URL de retorno inválida.", dto.ReturnUrl, dto.ClientId));
            }

            if (hasReturnUrl)
            {
                if (!_clientService.IsReturnUrlAllowed(client, dto.ReturnUrl))
                {
                    return await FinalizeResultAsync(BuildLoginRedirect("invalid_return_url", "URL de retorno inválida.", dto.ReturnUrl, dto.ClientId));
                }

                returnUrl = dto.ReturnUrl;
            }
            else
            {
                returnUrl = routing.Value.ReturnUrl;
                if (string.IsNullOrWhiteSpace(returnUrl) || !_clientService.IsReturnUrlAllowed(client, returnUrl))
                {
                    return await FinalizeResultAsync(BuildLoginRedirect("invalid_return_url", "Aplicación o URL de retorno inválida.", dto.ReturnUrl, dto.ClientId));
                }
            }
        }

        var rawPermissions = await _permissionService.GetAsync(
            user.UserName!,
            clientId: client?.Id,
            roleNamesOverride: effectiveRoles,
            areaIdsOverride: testAreaId.HasValue ? new[] { testAreaId.Value } : null);
        var apps = new List<string> { clientId };
        var claimsModel = _permissionsAssembler.BuildClaims(user, effectiveRoles, rawPermissions, apps);
        claimsModel = MergeTestUserClaims(claimsModel, testUser);
        await SignInAsync(user, claimsModel);

        _logger.LogInformation("Login exitoso: user='{User}' → redirectUrl='{RedirectUrl}' permsVersion={Version}",
            user.UserName, returnUrl, rawPermissions.Version);

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

    private async Task SignInAsync(ApplicationUser user, AuthClaimsModel claimsModel)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            throw new InvalidOperationException("HttpContext no disponible para iniciar sesión.");
        }

        var basePrincipal = await _claimsPrincipalFactory.CreateAsync(user);
        var baseIdentity = basePrincipal.Identity as ClaimsIdentity;
        var identity = new ClaimsIdentity(
            basePrincipal.Claims,
            IdentityConstants.ApplicationScheme,
            baseIdentity?.NameClaimType ?? ClaimTypes.Name,
            baseIdentity?.RoleClaimType ?? ClaimTypes.Role);

        RemoveClaimsOfType(identity, ClaimTypes.NameIdentifier);
        RemoveClaimsOfType(identity, ClaimTypes.Name);
        RemoveClaimsOfType(identity, ClaimTypes.Email);
        RemoveClaimsOfType(identity, ClaimTypes.Role);
        RemoveClaimsOfType(identity, "area");
        RemoveClaimsOfType(identity, "app");
        RemoveClaimsOfType(identity, "perms_json");
        RemoveClaimsOfType(identity, "perms_version");

        var displayName = claimsModel.DisplayName;
        if (!string.IsNullOrWhiteSpace(claimsModel.Email)
            && !string.IsNullOrWhiteSpace(displayName)
            && displayName.Equals(claimsModel.Email, StringComparison.OrdinalIgnoreCase))
        {
            displayName = displayName.Split('@')[0];
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, claimsModel.UserId),
            new(ClaimTypes.Name, displayName ?? claimsModel.UserId)
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

        claims.Add(new Claim("perms_version", claimsModel.PermissionsVersion.ToString()));

        var permissionsJson = string.IsNullOrWhiteSpace(claimsModel.PermissionsJson)
            ? "{\"pages\":[],\"version\":" + claimsModel.PermissionsVersion + "}"
            : claimsModel.PermissionsJson;
        claims.Add(new Claim("perms_json", permissionsJson));

        foreach (var app in claimsModel.Apps)
        {
            claims.Add(new Claim("app", app));
        }

        identity.AddClaims(claims);
        var principal = new ClaimsPrincipal(identity);
        var authProps = new AuthenticationProperties
        {
            IsPersistent = false,
            AllowRefresh = true
        };

        await _authenticationService.SignInAsync(httpContext, IdentityConstants.ApplicationScheme, principal, authProps);
    }

    private static void RemoveClaimsOfType(ClaimsIdentity identity, string claimType)
    {
        var existing = identity.Claims.Where(c => string.Equals(c.Type, claimType, StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var claim in existing)
        {
            identity.RemoveClaim(claim);
        }
    }

    private TestUserOptions? TryGetTestUser(string userNameOrEmail)
    {
        if (_testUsers.Users is null || _testUsers.Users.Count == 0)
        {
            return null;
        }

        var normalized = userNameOrEmail.Trim();
        var localPart = normalized.Contains('@') ? normalized.Split('@')[0] : normalized;

        foreach (var candidate in _testUsers.Users)
        {
            if (string.IsNullOrWhiteSpace(candidate.Email))
            {
                continue;
            }

            if (normalized.Equals(candidate.Email, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }

            var candidateLocal = candidate.Email.Split('@')[0];
            if (localPart.Equals(candidateLocal, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return null;
    }

    private static AuthClaimsModel MergeTestUserClaims(AuthClaimsModel claimsModel, TestUserOptions? testUser)
    {
        if (testUser is null)
        {
            return claimsModel;
        }

        var mergedRoles = claimsModel.Roles
            .Concat(testUser.Roles ?? new List<string>())
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var mergedAreas = claimsModel.Areas
            .Concat(string.IsNullOrWhiteSpace(testUser.Area) ? Array.Empty<string>() : new[] { testUser.Area })
            .Where(area => !string.IsNullOrWhiteSpace(area))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var mergedDisplayName = claimsModel.DisplayName;
        if (!string.IsNullOrWhiteSpace(testUser.DisplayName))
        {
            if (string.IsNullOrWhiteSpace(mergedDisplayName)
                || mergedDisplayName.Contains('@')
                || (claimsModel.Email is not null && mergedDisplayName.Equals(claimsModel.Email, StringComparison.OrdinalIgnoreCase)))
            {
                mergedDisplayName = testUser.DisplayName;
            }
        }

        return new AuthClaimsModel
        {
            UserId = claimsModel.UserId,
            Email = claimsModel.Email,
            DisplayName = mergedDisplayName,
            Roles = mergedRoles,
            Areas = mergedAreas,
            Apps = claimsModel.Apps,
            Pages = claimsModel.Pages,
            PermissionsVersion = claimsModel.PermissionsVersion,
            PermissionsJson = claimsModel.PermissionsJson,
            FirstPageUrl = claimsModel.FirstPageUrl
        };
    }

    private static bool PasswordMatches(string inputPassword, string expectedPassword)
    {
        if (string.IsNullOrEmpty(expectedPassword))
        {
            return false;
        }

        // Use constant-time comparison to prevent timing oracle attacks.
        var inputBytes = System.Text.Encoding.UTF8.GetBytes(inputPassword ?? string.Empty);
        var expectedBytes = System.Text.Encoding.UTF8.GetBytes(expectedPassword);
        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(inputBytes, expectedBytes);
    }

    private static bool VerifyPasswordHash(string? passwordHash, string inputPassword)
    {
        if (string.IsNullOrEmpty(passwordHash))
        {
            return false;
        }

        var hasher = new PasswordHasher<ApplicationUser>();
        var dummy = new ApplicationUser();
        var result = hasher.VerifyHashedPassword(dummy, passwordHash, inputPassword ?? string.Empty);
        return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded;
    }
}
