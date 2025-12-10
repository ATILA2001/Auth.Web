using Auth.Web.Controllers;
using Auth.Web.Services.Abstractions.Auth;
using Auth.Web.Application.Dtos;
using Auth.Web.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Auth.Web.Tests;

public class ConnectControllerTests
{
    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock(ApplicationUser user)
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var mgr = new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mgr.Setup(x => x.FindByNameAsync(user.UserName!)).ReturnsAsync(user);
        mgr.Setup(x => x.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
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

    [Fact]
    public async Task Login_Admin_Redirects_To_Admin()
    {
        var user = new ApplicationUser { Id = "u1", UserName = "admin@corp", Email = "admin@corp" };
        var authFlow = new Mock<IAuthFlowService>();
        authFlow.Setup(x => x.LoginAsync(It.IsAny<LoginRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(LoginOutcome.Admin("/admin"));
        var umMock = CreateUserManagerMock(user);
        var smMock = CreateSignInManagerMock(umMock.Object);
        var controller = new ConnectController(authFlow.Object, umMock.Object, smMock.Object);
        var result = await controller.Login(new LoginRequestDto { UserNameOrEmail = user.UserName!, Password = "pwd" }, CancellationToken.None);
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/admin", redirect.Url);
    }

    [Fact]
    public async Task Login_External_Redirects_To_Target()
    {
        var user = new ApplicationUser { Id = "u2", UserName = "user@corp", Email = "user@corp" };
        var authFlow = new Mock<IAuthFlowService>();
        authFlow.Setup(x => x.LoginAsync(It.IsAny<LoginRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(LoginOutcome.ExternalApp("https://app?token=XYZ"));
        var umMock = CreateUserManagerMock(user);
        var smMock = CreateSignInManagerMock(umMock.Object);
        var controller = new ConnectController(authFlow.Object, umMock.Object, smMock.Object);
        var result = await controller.Login(new LoginRequestDto { UserNameOrEmail = user.UserName!, Password = "pwd" }, CancellationToken.None);
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("https://app?token=XYZ", redirect.Url);
    }

    [Fact]
    public async Task Login_Failure_Redirects_To_Login_With_Error()
    {
        var user = new ApplicationUser { Id = "u3", UserName = "user3@corp", Email = "user3@corp" };
        var authFlow = new Mock<IAuthFlowService>();
        authFlow.Setup(x => x.LoginAsync(It.IsAny<LoginRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(LoginOutcome.Failure("Credenciales inválidas"));
        var umMock = CreateUserManagerMock(user);
        var smMock = CreateSignInManagerMock(umMock.Object);
        var controller = new ConnectController(authFlow.Object, umMock.Object, smMock.Object);
        var result = await controller.Login(new LoginRequestDto { UserNameOrEmail = user.UserName!, Password = "bad" }, CancellationToken.None);
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Contains("/Account/Login", redirect.Url);
        Assert.Contains("error=", redirect.Url);
    }
}
