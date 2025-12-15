using Auth.Web.Controllers;
using Auth.Web.Services.Abstractions.Auth;
using Auth.Web.Contracts.Auth;
using Auth.Web.Services.Abstractions.Auth.Models;
using Auth.Web.Services.Abstractions.Clients;
using Auth.Web.Services.Abstractions.Permissions;
using Auth.Web.Application.Permissions;
using Auth.Web.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;

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
        mgr.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin" });
        return mgr;
    }

    private static Mock<SignInManager<ApplicationUser>> CreateSignInManagerMock(UserManager<ApplicationUser> um)
    {
        var contextAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        var sm = new Mock<SignInManager<ApplicationUser>>(um, contextAccessor.Object, claimsFactory.Object, null!, null!, null!, null!);
        sm.Setup(s => s.SignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<bool>(), null)).Returns(Task.CompletedTask);
        return sm;
    }

    private static ConnectController CreateController(IAuthFlowService authFlow, ApplicationUser user)
    {
        var umMock = CreateUserManagerMock(user);
        var smMock = CreateSignInManagerMock(umMock.Object);
        return new ConnectController(authFlow, umMock.Object, smMock.Object);
    }

    [Fact]
    public async Task Login_Admin_Redirects_To_Admin()
    {
        var user = new ApplicationUser { Id = "u1", UserName = "admin@corp", Email = "admin@corp" };
        var authFlow = new Mock<IAuthFlowService>();
        authFlow.Setup(x => x.LoginAsync(It.IsAny<LoginRequestDto>()))
            .ReturnsAsync(new LoginResult { SignInAdmin = true, AdminUserId = user.Id, RedirectUrl = "/admin" });
        var controller = CreateController(authFlow.Object, user);
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
        var controller = CreateController(authFlow.Object, user);
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
        var controller = CreateController(authFlow.Object, user);
        var result = await controller.Login(new LoginRequestDto { UserNameOrEmail = user.UserName!, Password = "bad" });
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Contains("/Account/Login", redirect.Url);
        Assert.Contains("error=", redirect.Url);
    }
}
