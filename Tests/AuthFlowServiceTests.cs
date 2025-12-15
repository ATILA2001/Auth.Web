using System.Threading;
using AuthClaimsModel = Auth.Web.Application.Auth.AuthClaimsModel;
using Auth.Web.Services.Abstractions.Auth;
using Auth.Web.Services.Abstractions.Users;
using Auth.Web.Services.Abstractions.Permissions;
using Auth.Web.Services.Abstractions.Routing;
using Auth.Web.Services.Abstractions.Clients;
using Auth.Web.Domain.Dtos;
using Auth.Web.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;
using Auth.Web.Contracts.Auth;
using Auth.Web.Services.Abstractions.Auth.Models;
using Auth.Web.Application.Permissions;

namespace Auth.Web.Tests;

public class AuthFlowServiceTests
{
    private static Services.Implementations.Auth.AuthFlowService CreateService(
        Mock<IActiveDirectoryAuthService>? ad = null,
        Mock<IClientService>? client = null,
        Mock<IRoutingService>? routing = null,
        Mock<IPermissionService>? perms = null,
        Mock<IJwtTokenService>? jwt = null,
        Mock<IUserManagementService>? userManagement = null,
        Mock<IUserProvisioningService>? provisioning = null,
        UserPermissionsAssembler? assembler = null,
        Mock<IAdminSignInService>? adminSignIn = null)
    {
        ad ??= new Mock<IActiveDirectoryAuthService>();
        client ??= new Mock<IClientService>();
        routing ??= new Mock<IRoutingService>();
        perms ??= new Mock<IPermissionService>();
        jwt ??= new Mock<IJwtTokenService>();

        userManagement ??= new Mock<IUserManagementService>();
        provisioning ??= new Mock<IUserProvisioningService>();
        assembler ??= new UserPermissionsAssembler();
        adminSignIn ??= new Mock<IAdminSignInService>();

        return new Services.Implementations.Auth.AuthFlowService(ad.Object, userManagement.Object, perms.Object, routing.Object, client.Object, jwt.Object, provisioning.Object, assembler, adminSignIn.Object);
    }

    private static ApplicationUser MakeUser(string id, string name) => new ApplicationUser { Id = id, UserName = name, Email = name };

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
        var user = MakeUser("u1", "admin@corp");
        userManagement.Setup(x => x.FindByNameAsync(user.UserName)).ReturnsAsync(user);
        userManagement.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        userManagement.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin" });

        var adminSignIn = new Mock<IAdminSignInService>();
        var svc = CreateService(ad: ad, userManagement: userManagement, adminSignIn: adminSignIn);
        var outcome = await svc.LoginAsync(new LoginRequestDto { UserNameOrEmail = user.UserName!, Password = "pwd" });
        Assert.True(outcome.SignInAdmin);
        Assert.Equal("/admin", outcome.RedirectUrl);
        Assert.Equal(user.Id, outcome.AdminUserId);
        adminSignIn.Verify(x => x.SignInAsync(user.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Login_NonAdmin_SuccessExternalWithToken()
    {
        var ad = new Mock<IActiveDirectoryAuthService>();
        ad.Setup(x => x.ValidateCredentialsAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

        var userManagement = new Mock<IUserManagementService>();
        var user = MakeUser("u2", "user@corp");
        userManagement.Setup(x => x.FindByNameAsync(user.UserName)).ReturnsAsync(user);
        userManagement.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        userManagement.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Usuario" });

        var routing = new Mock<IRoutingService>();
        routing.Setup(x => x.ResolveForUserAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(("clientA", "https://app/landing"));

        var clientSvc = new Mock<IClientService>();
        var client = new ApplicationClient { Audience = "audA", ClientId = "clientA" };
        clientSvc.Setup(x => x.GetAsync("clientA")).ReturnsAsync(client);
        clientSvc.Setup(x => x.IsReturnUrlAllowed(client, "https://app/landing")).Returns(true);

        var perms = new Mock<IPermissionService>();
        perms.Setup(x => x.GetAsync(user.UserName!)).ReturnsAsync(new Auth.Web.Domain.Dtos.UserPermissionsDto { Areas = new List<int> { 1 }, Version = 1 });

        var jwt = new Mock<IJwtTokenService>();
        jwt.Setup(x => x.CreateToken(It.IsAny<AuthClaimsModel>(), client.Audience)).Returns("TOKEN_X");

        var assembler = new UserPermissionsAssembler();

        var svc = CreateService(ad: ad, client: clientSvc, routing: routing, perms: perms, jwt: jwt, userManagement: userManagement, assembler: assembler);
        var outcome = await svc.LoginAsync(new LoginRequestDto { UserNameOrEmail = user.UserName!, Password = "pwd" });

        Assert.False(outcome.SignInAdmin);
        Assert.NotNull(outcome.RedirectUrl);
        Assert.Contains("token=TOKEN_X", outcome.RedirectUrl);
    }

    [Fact]
    public async Task Login_NoRouting_Failure()
    {
        var ad = new Mock<IActiveDirectoryAuthService>();
        ad.Setup(x => x.ValidateCredentialsAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
        var userManagement = new Mock<IUserManagementService>();
        var user = MakeUser("u3", "user3@corp");
        userManagement.Setup(x => x.FindByNameAsync(user.UserName)).ReturnsAsync(user);
        userManagement.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        userManagement.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Usuario" });

        var routing = new Mock<IRoutingService>();
        routing.Setup(x => x.ResolveForUserAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(((string ClientId, string ReturnUrl)?)null);

        var svc = CreateService(ad: ad, routing: routing, userManagement: userManagement);
        var outcome = await svc.LoginAsync(new LoginRequestDto { UserNameOrEmail = user.UserName!, Password = "pwd" });
        Assert.False(outcome.SignInAdmin);
        Assert.Contains("/Account/Login", outcome.RedirectUrl);
    }

    [Fact]
    public async Task Login_InvalidReturnUrl_Failure()
    {
        var ad = new Mock<IActiveDirectoryAuthService>();
        ad.Setup(x => x.ValidateCredentialsAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
        var userManagement = new Mock<IUserManagementService>();
        var user = MakeUser("u4", "user4@corp");
        userManagement.Setup(x => x.FindByNameAsync(user.UserName)).ReturnsAsync(user);
        userManagement.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        userManagement.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Usuario" });

        var routing = new Mock<IRoutingService>();
        routing.Setup(x => x.ResolveForUserAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(("clientB", "https://bad/forbidden"));

        var clientSvc = new Mock<IClientService>();
        var client = new ApplicationClient { Audience = "audB", ClientId = "clientB" };
        clientSvc.Setup(x => x.GetAsync("clientB")).ReturnsAsync(client);
        clientSvc.Setup(x => x.IsReturnUrlAllowed(client, "https://bad/forbidden")).Returns(false);

        var svc = CreateService(ad: ad, client: clientSvc, routing: routing, userManagement: userManagement);
        var outcome = await svc.LoginAsync(new LoginRequestDto { UserNameOrEmail = user.UserName!, Password = "pwd" });

        Assert.StartsWith("/Account/Login?", outcome.RedirectUrl);
        var uri = new Uri("http://local" + outcome.RedirectUrl);
        var qs = System.Web.HttpUtility.ParseQueryString(uri.Query);
        Assert.False(string.IsNullOrWhiteSpace(qs["error"]));
        Assert.Equal("invalid_return_url", qs["errorCode"]);
    }
}
