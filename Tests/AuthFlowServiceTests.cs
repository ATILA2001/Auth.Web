using System.Threading;
using Auth.Web.Application.Permissions;
using Auth.Web.Application.Permissions.Dtos;
using Auth.Web.Contracts.Auth;
using Auth.Web.Data.Entities;
using Auth.Web.Services.Abstractions.Auth;
using Auth.Web.Services.Abstractions.Auth.Models;
using Auth.Web.Services.Abstractions.Clients;
using Auth.Web.Services.Abstractions.Permissions;
using Auth.Web.Services.Abstractions.Routing;
using Auth.Web.Services.Abstractions.Users;
using Auth.Web.Services.Implementations.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Moq;
using Xunit;

namespace Auth.Web.Tests;

public class AuthFlowServiceTests
{
    private static AuthFlowService CreateService(
        Mock<IActiveDirectoryAuthService>? ad = null,
        Mock<IClientService>? client = null,
        Mock<IRoutingService>? routing = null,
        Mock<IPermissionService>? perms = null,
        Mock<IUserManagementService>? userManagement = null,
        Mock<IUserProvisioningService>? provisioning = null,
        UserPermissionsAssembler? assembler = null,
        Mock<IAdminSignInService>? adminSignIn = null,
        Mock<IAuthenticationService>? authService = null,
        Mock<IUserClaimsPrincipalFactory<ApplicationUser>>? claimsFactory = null,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        ad ??= new Mock<IActiveDirectoryAuthService>();
        client ??= new Mock<IClientService>();
        routing ??= new Mock<IRoutingService>();
        perms ??= new Mock<IPermissionService>();

        userManagement ??= new Mock<IUserManagementService>();
        provisioning ??= new Mock<IUserProvisioningService>();
        assembler ??= new UserPermissionsAssembler();
        adminSignIn ??= new Mock<IAdminSignInService>();

        var ctx = new DefaultHttpContext();
        httpContextAccessor ??= new HttpContextAccessor { HttpContext = ctx };
        authService ??= new Mock<IAuthenticationService>();
        claimsFactory ??= new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        claimsFactory
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(IdentityConstants.ApplicationScheme)));

        var options = Microsoft.Extensions.Options.Options.Create(new Auth.Web.Configuration.TestUsersOptions());
        var featureOptions = Microsoft.Extensions.Options.Options.Create(new Auth.Web.Configuration.FeatureOptions { EnableTestUsers = true });
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthFlowService>.Instance;
        var userStore = new Mock<Microsoft.AspNetCore.Identity.IUserStore<ApplicationUser>>();
        var userManager = new Mock<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>(
            userStore.Object,
            Microsoft.Extensions.Options.Options.Create(new Microsoft.AspNetCore.Identity.IdentityOptions()),
            Mock.Of<Microsoft.AspNetCore.Identity.IPasswordHasher<ApplicationUser>>(),
            Array.Empty<Microsoft.AspNetCore.Identity.IUserValidator<ApplicationUser>>(),
            Array.Empty<Microsoft.AspNetCore.Identity.IPasswordValidator<ApplicationUser>>(),
            Mock.Of<Microsoft.AspNetCore.Identity.ILookupNormalizer>(),
            new Microsoft.AspNetCore.Identity.IdentityErrorDescriber(),
            Mock.Of<IServiceProvider>(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>.Instance);
        return new AuthFlowService(ad.Object, userManagement.Object, perms.Object, routing.Object, client.Object, provisioning.Object, assembler, adminSignIn.Object, authService.Object, claimsFactory.Object, httpContextAccessor, options, featureOptions, userManager.Object, logger);
    }

    private static (ApplicationUser User, string UserName, string Email) MakeUser(string id, string userName, string email)
    {
        ArgumentNullException.ThrowIfNull(userName);
        ArgumentNullException.ThrowIfNull(email);
        var user = new ApplicationUser { Id = id, UserName = userName, Email = email };
        return (user, userName, email);
    }

    [Fact]
    public async Task Login_Failure_InvalidCredentials()
    {
        var ad = new Mock<IActiveDirectoryAuthService>();
        ad.Setup(x => x.ValidateCredentialsAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
        var svc = CreateService(ad: ad);
        var outcome = await svc.LoginAsync(new LoginRequestDto { UserNameOrEmail = "user", Password = "pwd" });
        Assert.Equal("/Account/Login", outcome.RedirectUrl[..14]);
        Assert.False(outcome.SignInAdmin);
    }

    [Fact]
    public async Task Login_AdminSuccess()
    {
        var ad = new Mock<IActiveDirectoryAuthService>();
        ad.Setup(x => x.ValidateCredentialsAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

        var userManagement = new Mock<IUserManagementService>();
        var (user, userName, email) = MakeUser("u1", "admin@corp", "admin@corp");
        userManagement.Setup(x => x.FindByNameAsync(userName)).ReturnsAsync(user);
        userManagement.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync(user);
        userManagement.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin" });

        var adminSignIn = new Mock<IAdminSignInService>();
        var routing = new Mock<IRoutingService>();
        routing.Setup(x => x.ResolveAllForUserAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(string ClientId, string ReturnUrl)> { ("SAI", "https://app/admin") });

        var perms = new Mock<IPermissionService>();
        perms.Setup(x => x.GetAsync(userName, It.IsAny<int?>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<IReadOnlyCollection<int>>()))
            .ReturnsAsync(new UserPermissionsDto { AreaIds = new List<int>(), Version = 1 });

        var authService = new Mock<IAuthenticationService>();

        var svc = CreateService(ad: ad, userManagement: userManagement, routing: routing, adminSignIn: adminSignIn, perms: perms, authService: authService);
        var outcome = await svc.LoginAsync(new LoginRequestDto { UserNameOrEmail = userName, Password = "pwd" });
        Assert.True(outcome.ShowAppPicker);
        Assert.False(outcome.SignInAdmin);
        Assert.True(string.IsNullOrWhiteSpace(outcome.AdminUserId));
        adminSignIn.Verify(x => x.SignInAsync(user.Id, It.IsAny<CancellationToken>()), Times.Never);
        authService.Verify(x => x.SignInAsync(It.IsAny<HttpContext>(), IdentityConstants.ApplicationScheme, It.IsAny<System.Security.Claims.ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()), Times.Once);
    }

    [Fact]
    public async Task Login_NonAdmin_Success_With_Cookie_SignIn()
    {
        var ad = new Mock<IActiveDirectoryAuthService>();
        ad.Setup(x => x.ValidateCredentialsAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

        var userManagement = new Mock<IUserManagementService>();
        var (user, userName, email) = MakeUser("u2", "user@corp", "user@corp");
        userManagement.Setup(x => x.FindByNameAsync(userName)).ReturnsAsync(user);
        userManagement.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync(user);
        userManagement.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Usuario" });

        var routing = new Mock<IRoutingService>();
        routing.Setup(x => x.ResolveAllForUserAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(string ClientId, string ReturnUrl)> { ("clientA", "https://app/landing") });

        var clientSvc = new Mock<IClientService>();
        var client = new ApplicationClient { Audience = "audA", ClientId = "clientA" };
        clientSvc.Setup(x => x.GetAsync("clientA")).ReturnsAsync(client);
        clientSvc.Setup(x => x.IsReturnUrlAllowed(client, "https://app/landing")).Returns(true);
        clientSvc.Setup(x => x.ResolveReturnUrlForCurrentEnvironment(client, "https://app/landing", It.IsAny<string?>()))
            .Returns("https://app/landing");

        var perms = new Mock<IPermissionService>();
        perms.Setup(x => x.GetAsync(userName, It.IsAny<int?>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<IReadOnlyCollection<int>>()))
            .ReturnsAsync(new UserPermissionsDto { AreaIds = new List<int> { 1 }, Version = 1 });

        var authService = new Mock<IAuthenticationService>();
        var assembler = new UserPermissionsAssembler();

        var svc = CreateService(ad: ad, client: clientSvc, routing: routing, perms: perms, userManagement: userManagement, assembler: assembler, authService: authService);
        var outcome = await svc.LoginAsync(new LoginRequestDto { UserNameOrEmail = userName, Password = "pwd" });

        Assert.False(outcome.SignInAdmin);
        Assert.NotNull(outcome.RedirectUrl);
        Assert.Equal("https://app/landing", outcome.RedirectUrl);
        authService.Verify(x => x.SignInAsync(It.IsAny<HttpContext>(), IdentityConstants.ApplicationScheme, It.IsAny<System.Security.Claims.ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()), Times.Once);
    }

    [Fact]
    public async Task Login_With_ReturnUrl_Infers_Client_From_Allowed_Urls()
    {
        var ad = new Mock<IActiveDirectoryAuthService>();
        ad.Setup(x => x.ValidateCredentialsAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

        var userManagement = new Mock<IUserManagementService>();
        var (user, userName, email) = MakeUser("u5", "user5@corp", "user5@corp");
        userManagement.Setup(x => x.FindByNameAsync(userName)).ReturnsAsync(user);
        userManagement.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync(user);
        userManagement.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Usuario" });

        var returnUrl = "https://sai.test/inventory";
        var inferredClient = new ApplicationClient { Id = 42, Audience = "SAI TEST", ClientId = "SAI TEST" };
        var otherClient = new ApplicationClient { Id = 43, Audience = "PLAFI TEST", ClientId = "Plani TEST" };

        var clientSvc = new Mock<IClientService>();
        clientSvc.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { inferredClient, otherClient });
        clientSvc.Setup(x => x.IsReturnUrlAllowed(inferredClient, returnUrl)).Returns(true);
        clientSvc.Setup(x => x.IsReturnUrlAllowed(otherClient, returnUrl)).Returns(false);
        clientSvc.Setup(x => x.ResolveReturnUrlForCurrentEnvironment(inferredClient, returnUrl, It.IsAny<string?>()))
            .Returns(returnUrl);

        var routing = new Mock<IRoutingService>();
        routing.Setup(x => x.ResolveAllForUserAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(string ClientId, string ReturnUrl)> { ("SAI TEST", "https://sai.test/") });

        var perms = new Mock<IPermissionService>();
        perms.Setup(x => x.GetAsync(userName, inferredClient.Id, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<IReadOnlyCollection<int>>()))
            .ReturnsAsync(new UserPermissionsDto { AreaIds = new List<int> { 1 }, Version = 1 });

        var authService = new Mock<IAuthenticationService>();

        var svc = CreateService(ad: ad, client: clientSvc, routing: routing, perms: perms, userManagement: userManagement, authService: authService);
        var outcome = await svc.LoginAsync(new LoginRequestDto { UserNameOrEmail = userName, Password = "pwd", ReturnUrl = returnUrl });

        Assert.Equal(returnUrl, outcome.RedirectUrl);
        perms.Verify(x => x.GetAsync(userName, inferredClient.Id, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<IReadOnlyCollection<int>>()), Times.Once);
        authService.Verify(x => x.SignInAsync(It.IsAny<HttpContext>(), IdentityConstants.ApplicationScheme, It.IsAny<System.Security.Claims.ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()), Times.Once);
    }

    [Fact]
    public async Task Login_NoRouting_Failure()
    {
        var ad = new Mock<IActiveDirectoryAuthService>();
        ad.Setup(x => x.ValidateCredentialsAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
        var userManagement = new Mock<IUserManagementService>();
        var (user, userName, email) = MakeUser("u3", "user3@corp", "user3@corp");
        userManagement.Setup(x => x.FindByNameAsync(userName)).ReturnsAsync(user);
        userManagement.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync(user);
        userManagement.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Usuario" });

        var routing = new Mock<IRoutingService>();
        routing.Setup(x => x.ResolveAllForUserAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<(string ClientId, string ReturnUrl)>());

        var svc = CreateService(ad: ad, routing: routing, userManagement: userManagement);
        var outcome = await svc.LoginAsync(new LoginRequestDto { UserNameOrEmail = userName, Password = "pwd" });
        Assert.False(outcome.SignInAdmin);
        Assert.Contains("/Account/Login", outcome.RedirectUrl);
    }

    [Fact]
    public async Task Login_InvalidReturnUrl_Failure()
    {
        var ad = new Mock<IActiveDirectoryAuthService>();
        ad.Setup(x => x.ValidateCredentialsAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
        var userManagement = new Mock<IUserManagementService>();
        var (user, userName, email) = MakeUser("u4", "user4@corp", "user4@corp");
        userManagement.Setup(x => x.FindByNameAsync(userName)).ReturnsAsync(user);
        userManagement.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync(user);
        userManagement.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Usuario" });

        var routing = new Mock<IRoutingService>();
        routing.Setup(x => x.ResolveAllForUserAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(string ClientId, string ReturnUrl)> { ("clientB", "https://bad/forbidden") });

        var clientSvc = new Mock<IClientService>();
        var client = new ApplicationClient { Audience = "audB", ClientId = "clientB" };
        clientSvc.Setup(x => x.GetAsync("clientB")).ReturnsAsync(client);
        clientSvc.Setup(x => x.IsReturnUrlAllowed(client, "https://bad/forbidden")).Returns(false);

        var svc = CreateService(ad: ad, client: clientSvc, routing: routing, userManagement: userManagement);
        var outcome = await svc.LoginAsync(new LoginRequestDto { UserNameOrEmail = userName, Password = "pwd" });

        Assert.StartsWith("/Account/Login?", outcome.RedirectUrl);
        var uri = new Uri("http://local" + outcome.RedirectUrl);
        var qs = System.Web.HttpUtility.ParseQueryString(uri.Query);
        Assert.False(string.IsNullOrWhiteSpace(qs["error"]));
        Assert.Equal("invalid_return_url", qs["errorCode"]);
    }
}
