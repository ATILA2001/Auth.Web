using Auth.Web.Controllers;
using Auth.Web.Services.Abstractions.Auth;
using Auth.Web.Contracts.Auth;
using Auth.Web.Services.Abstractions.Auth.Models;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Auth.Web.Tests;

public class ConnectControllerTests
{
    private static ConnectController CreateController(
        IAuthFlowService authFlow,
        IAntiforgery? antiforgery = null,
        HttpContext? httpContext = null)
    {
        var controller = new ConnectController(
            authFlow,
            antiforgery ?? Mock.Of<IAntiforgery>());

        if (httpContext != null)
        {
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        return controller;
    }

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

    [Fact]
    public async Task PortalLogin_Delegates_To_Login()
    {
        var authFlow = new Mock<IAuthFlowService>();
        authFlow.Setup(x => x.LoginAsync(It.IsAny<LoginRequestDto>()))
            .ReturnsAsync(new LoginResult { SignInAdmin = false, RedirectUrl = "/portal" });

        var controller = CreateController(authFlow.Object);
        var dto = new LoginRequestDto { UserNameOrEmail = "portal@corp", Password = "pwd" };

        var result = await controller.PortalLogin(dto);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/portal", redirect.Url);
        authFlow.Verify(x => x.LoginAsync(dto), Times.Once);
    }

    [Fact]
    public void Logout_GET_Returns_405_MethodNotAllowed()
    {
        var authFlow = new Mock<IAuthFlowService>();
        var controller = CreateController(authFlow.Object);

        var result = controller.Logout();

        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status405MethodNotAllowed, statusResult.StatusCode);
    }

    [Fact]
    public async Task LogoutPost_Returns_BadRequest_When_Antiforgery_Fails()
    {
        var authFlow = new Mock<IAuthFlowService>();
        var antiforgeryMock = new Mock<IAntiforgery>();
        var httpContext = new DefaultHttpContext();

        // Configure services for the HttpContext
        var serviceProvider = new Mock<IServiceProvider>();
        httpContext.RequestServices = serviceProvider.Object;

        antiforgeryMock.Setup(a => a.ValidateRequestAsync(httpContext))
            .ThrowsAsync(new AntiforgeryValidationException("Invalid token"));

        var controller = CreateController(authFlow.Object, antiforgeryMock.Object, httpContext);

        var result = await controller.LogoutPost();

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task LogoutPost_Redirects_To_Login_When_Valid()
    {
        var authFlow = new Mock<IAuthFlowService>();
        var antiforgeryMock = new Mock<IAntiforgery>();
        var httpContext = new DefaultHttpContext();

        // Configure a minimal service provider with authentication service
        var authServiceMock = new Mock<Microsoft.AspNetCore.Authentication.IAuthenticationService>();
        authServiceMock.Setup(a => a.SignOutAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string>(),
                It.IsAny<Microsoft.AspNetCore.Authentication.AuthenticationProperties>()))
            .Returns(Task.CompletedTask);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(sp => sp.GetService(typeof(Microsoft.AspNetCore.Authentication.IAuthenticationService)))
            .Returns(authServiceMock.Object);

        httpContext.RequestServices = serviceProvider.Object;

        antiforgeryMock.Setup(a => a.ValidateRequestAsync(httpContext))
            .Returns(Task.CompletedTask);

        var controller = CreateController(authFlow.Object, antiforgeryMock.Object, httpContext);

        var result = await controller.LogoutPost();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/Account/Login", redirect.Url);
    }

    [Fact]
    public async Task Login_Calls_AuthFlowService_With_Correct_Dto()
    {
        var authFlow = new Mock<IAuthFlowService>();
        LoginRequestDto? capturedDto = null;

        authFlow.Setup(x => x.LoginAsync(It.IsAny<LoginRequestDto>()))
            .Callback<LoginRequestDto>(dto => capturedDto = dto)
            .ReturnsAsync(new LoginResult { RedirectUrl = "/test" });

        var controller = CreateController(authFlow.Object);
        var inputDto = new LoginRequestDto
        {
            UserNameOrEmail = "test@example.com",
            Password = "testpass"
        };

        await controller.Login(inputDto);

        Assert.NotNull(capturedDto);
        Assert.Equal("test@example.com", capturedDto.UserNameOrEmail);
        Assert.Equal("testpass", capturedDto.Password);
    }
}
