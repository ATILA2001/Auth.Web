using Auth.Web.Controllers;
using Auth.Web.Services.Abstractions.Auth;
using Auth.Web.Contracts.Auth;
using Auth.Web.Services.Abstractions.Auth.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Auth.Web.Tests;

public class ConnectControllerTests
{
    private static ConnectController CreateController(IAuthFlowService authFlow)
        => new ConnectController(authFlow);

    [Fact]
    public async Task Login_Admin_Redirects_To_Admin()
    {
        var authFlow = new Mock<IAuthFlowService>();
        authFlow.Setup(x => x.LoginAsync(It.IsAny<LoginRequestDto>()))
            .ReturnsAsync(new LoginResult { SignInAdmin = true, AdminUserId = "u1", RedirectUrl = "/admin" });
        var controller = CreateController(authFlow.Object);
        var result = await controller.Login(new LoginRequestDto { UserNameOrEmail = "admin@corp", Password = "pwd" });
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/admin", redirect.Url);
    }

    [Fact]
    public async Task Login_External_Redirects_To_Target()
    {
        var authFlow = new Mock<IAuthFlowService>();
        authFlow.Setup(x => x.LoginAsync(It.IsAny<LoginRequestDto>()))
            .ReturnsAsync(new LoginResult { SignInAdmin = false, RedirectUrl = "https://app?token=XYZ" });
        var controller = CreateController(authFlow.Object);
        var result = await controller.Login(new LoginRequestDto { UserNameOrEmail = "user@corp", Password = "pwd" });
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("https://app?token=XYZ", redirect.Url);
    }

    [Fact]
    public async Task Login_Failure_Redirects_To_Login_With_Error()
    {
        var authFlow = new Mock<IAuthFlowService>();
        authFlow.Setup(x => x.LoginAsync(It.IsAny<LoginRequestDto>()))
            .ReturnsAsync(new LoginResult { RedirectUrl = "/Account/Login?error=X" });
        var controller = CreateController(authFlow.Object);
        var result = await controller.Login(new LoginRequestDto { UserNameOrEmail = "user3@corp", Password = "bad" });
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Contains("/Account/Login", redirect.Url);
        Assert.Contains("error=", redirect.Url);
    }
}
