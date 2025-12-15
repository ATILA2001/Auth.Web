using Auth.Web.Controllers;
using Auth.Web.Services.Abstractions.Auth;
using Auth.Web.Contracts.Auth;
using Auth.Web.Services.Abstractions.Auth.Models;
using Auth.Web.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Auth.Web.Services.Abstractions.Clients;
using Auth.Web.Services.Abstractions.Permissions;
using Auth.Web.Application.Permissions;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Auth.Web.Tests;

public class ConnectControllerTests
{
    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock(ApplicationUser user)
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var mgr = new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mgr.Setup(x => x.FindByNameAsync(user.UserName!)).ReturnsAsync(user);
        mgr.Setup(x => x.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        mgr.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);
        mgr.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(new List<string> { "Admin" });
        return mgr;
    }

    private static Mock<SignInManager<ApplicationUser>> CreateSignInManagerMock(UserManager<ApplicationUser> um)
    {
        var contextAccessor = new Mock<IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        var sm = new Mock<SignInManager<ApplicationUser>>(um, contextAccessor.Object, claimsFactory.Object, null!, null!, null!, null!);
        sm.Setup(s => s.SignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<bool>(), null)).Returns(Task.CompletedTask);
        return sm;
    }

    private static ConnectController CreateController(
        IAuthFlowService authFlow,
        UserManager<ApplicationUser> um,
        SignInManager<ApplicationUser> sm,
        IClientService? clientService = null,
        IPermissionService? permissionService = null,
        IJwtTokenService? jwtTokenService = null,
        UserPermissionsAssembler? permissionsAssembler = null,
        ILogger<ConnectController>? logger = null)
    {
        clientService ??= Mock.Of<IClientService>();
        permissionService ??= Mock.Of<IPermissionService>();
        jwtTokenService ??= Mock.Of<IJwtTokenService>();
        permissionsAssembler ??= new UserPermissionsAssembler();
        logger ??= Mock.Of<ILogger<ConnectController>>();
        return new ConnectController(authFlow, um, sm, clientService, permissionService, jwtTokenService, permissionsAssembler, logger)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task Login_Admin_Redirects_To_Admin()
    {
        var user = new ApplicationUser { Id = "u1", UserName = "admin@corp", Email = "admin@corp" };
        var authFlow = new Mock<IAuthFlowService>();
        authFlow.Setup(x => x.LoginAsync(It.IsAny<LoginRequestDto>()))
            .ReturnsAsync(new LoginResult { SignInAdmin = true, AdminUserId = user.Id, RedirectUrl = "/admin" });
        var umMock = CreateUserManagerMock(user);
        var smMock = CreateSignInManagerMock(umMock.Object);
        var controller = CreateController(authFlow.Object, umMock.Object, smMock.Object);
        var result = await controller.Login(new LoginRequestDto { UserNameOrEmail = user.UserName!, Password = "pwd" });
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/admin", redirect.Url);
    }

    [Fact]
    public async Task Login_External_Redirects_To_Target()
    {
        var user = new ApplicationUser { Id = "u2", UserName = "user@corp", Email = "user@corp" };
        var authFlow = new Mock<IAuthFlowService>();
        authFlow.Setup(x => x.LoginAsync(It.IsAny<LoginRequestDto>()))
            .ReturnsAsync(new LoginResult { SignInAdmin = false, RedirectUrl = "https://app?token=XYZ" });
        var umMock = CreateUserManagerMock(user);
        var smMock = CreateSignInManagerMock(umMock.Object);
        var controller = CreateController(authFlow.Object, umMock.Object, smMock.Object);
        var result = await controller.Login(new LoginRequestDto { UserNameOrEmail = user.UserName!, Password = "pwd" });
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("https://app?token=XYZ", redirect.Url);
    }

    [Fact]
    public async Task Login_Failure_Redirects_To_Login_With_Error()
    {
        var user = new ApplicationUser { Id = "u3", UserName = "user3@corp", Email = "user3@corp" };
        var authFlow = new Mock<IAuthFlowService>();
        authFlow.Setup(x => x.LoginAsync(It.IsAny<LoginRequestDto>()))
            .ReturnsAsync(new LoginResult { RedirectUrl = "/Account/Login?error=X" });
        var umMock = CreateUserManagerMock(user);
        var smMock = CreateSignInManagerMock(umMock.Object);
        var controller = CreateController(authFlow.Object, umMock.Object, smMock.Object);
        var result = await controller.Login(new LoginRequestDto { UserNameOrEmail = user.UserName!, Password = "bad" });
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Contains("/Account/Login", redirect.Url);
        Assert.Contains("error=", redirect.Url);
    }

    // Launch endpoint tests

    [Fact]
    public async Task Launch_MissingParams_Redirects_To_Admin_With_ErrorCode()
    {
        var user = new ApplicationUser { Id = "u10", UserName = "admin@corp", Email = "admin@corp" };
        var authFlow = Mock.Of<IAuthFlowService>();
        var umMock = CreateUserManagerMock(user);
        var smMock = CreateSignInManagerMock(umMock.Object);
        var clientSvc = Mock.Of<IClientService>();
        var controller = CreateController(authFlow, umMock.Object, smMock.Object, clientSvc);
        var result = await controller.Launch(null, null);
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.StartsWith("/admin", redirect.Url);
        Assert.Contains("errorCode=missing_params", redirect.Url);
    }

    [Fact]
    public async Task Launch_NonAbsoluteReturnUrl_Redirects_InvalidReturnUrl()
    {
        var user = new ApplicationUser { Id = "u11", UserName = "admin@corp", Email = "admin@corp" };
        var authFlow = Mock.Of<IAuthFlowService>();
        var umMock = CreateUserManagerMock(user);
        var smMock = CreateSignInManagerMock(umMock.Object);
        var clientSvc = Mock.Of<IClientService>();
        var controller = CreateController(authFlow, umMock.Object, smMock.Object, clientSvc);
        var result = await controller.Launch("WebsiteV2", "/relative");
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.StartsWith("/admin", redirect.Url);
        Assert.Contains("errorCode=invalid_return_url", redirect.Url);
    }

    [Fact]
    public async Task Launch_InvalidClient_Redirects_InvalidClient()
    {
        var user = new ApplicationUser { Id = "u12", UserName = "admin@corp", Email = "admin@corp" };
        var authFlow = Mock.Of<IAuthFlowService>();
        var umMock = CreateUserManagerMock(user);
        var smMock = CreateSignInManagerMock(umMock.Object);
        var clientSvc = new Mock<IClientService>();
        clientSvc.Setup(s => s.GetAsync("WebsiteV2")).ReturnsAsync((ApplicationClient?)null);
        var controller = CreateController(authFlow, umMock.Object, smMock.Object, clientSvc.Object);
        var result = await controller.Launch("WebsiteV2", "http://app/landing");
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.StartsWith("/admin", redirect.Url);
        Assert.Contains("errorCode=invalid_client", redirect.Url);
    }

    [Fact]
    public async Task Launch_InvalidReturnUrl_Redirects_InvalidReturnUrl()
    {
        var user = new ApplicationUser { Id = "u13", UserName = "admin@corp", Email = "admin@corp" };
        var authFlow = Mock.Of<IAuthFlowService>();
        var umMock = CreateUserManagerMock(user);
        var smMock = CreateSignInManagerMock(umMock.Object);
        var clientSvc = new Mock<IClientService>();
        clientSvc.Setup(s => s.GetAsync("WebsiteV2")).ReturnsAsync(new ApplicationClient { ClientId = "WebsiteV2", Audience = "aud" });
        clientSvc.Setup(s => s.IsReturnUrlAllowed(It.IsAny<ApplicationClient>(), It.IsAny<string>())).Returns(false);
        var controller = CreateController(authFlow, umMock.Object, smMock.Object, clientSvc.Object);
        var result = await controller.Launch("WebsiteV2", "http://app/landing");
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.StartsWith("/admin", redirect.Url);
        Assert.Contains("errorCode=invalid_return_url", redirect.Url);
    }

    [Fact]
    public async Task Launch_Success_Redirects_To_ReturnUrl_With_Token()
    {
        var user = new ApplicationUser { Id = "u14", UserName = "admin@corp", Email = "admin@corp" };
        var authFlow = Mock.Of<IAuthFlowService>();
        var umMock = CreateUserManagerMock(user);
        var smMock = CreateSignInManagerMock(umMock.Object);
        var clientSvc = new Mock<IClientService>();
        clientSvc.Setup(s => s.GetAsync("WebsiteV2")).ReturnsAsync(new ApplicationClient { ClientId = "WebsiteV2", Audience = "audA" });
        clientSvc.Setup(s => s.IsReturnUrlAllowed(It.IsAny<ApplicationClient>(), It.IsAny<string>())).Returns(true);
        var permSvc = new Mock<IPermissionService>();
        permSvc.Setup(p => p.GetAsync(user.UserName!)).ReturnsAsync(new Domain.Dtos.UserPermissionsDto { Areas = new List<int> { 1 }, Version = 1 });
        var jwtSvc = new Mock<IJwtTokenService>();
        jwtSvc.Setup(j => j.CreateToken(It.IsAny<Application.Auth.AuthClaimsModel>(), "audA")).Returns("TOKEN_X");
        var logger = Mock.Of<ILogger<ConnectController>>();

        var controller = new ConnectController(authFlow, umMock.Object, smMock.Object, clientSvc.Object, permSvc.Object, jwtSvc.Object, new UserPermissionsAssembler(), logger)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        // Set identity with NameIdentifier claim
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, user.Id), new Claim(ClaimTypes.Role, "Admin") }, "TestAuth");
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);

        var result = await controller.Launch("WebsiteV2", "http://app/landing");
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.StartsWith("http://app/landing", redirect.Url);
        Assert.Contains("token=TOKEN_X", redirect.Url);
    }
}
