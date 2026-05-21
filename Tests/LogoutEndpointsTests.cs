using System.Security.Claims;
using Auth.Web.Controllers;
using Auth.Web.Services.Abstractions.Auth;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Auth.Web.Tests;

public class LogoutEndpointsTests
{
    [Fact]
    public void ConnectController_GetLogout_Returns405_AndDoesNotSignOut()
    {
        var authService = new FakeAuthenticationService();
        var controller = CreateController(authService);

        var result = controller.Logout();

        var status = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status405MethodNotAllowed, status.StatusCode);
        Assert.False(authService.SignedOut);
    }

    [Fact]
    public async Task ConnectController_PostLogout_WithoutAntiforgery_Fails()
    {
        var authService = new FakeAuthenticationService();
        var antiforgery = new Mock<IAntiforgery>();
        antiforgery
            .Setup(a => a.ValidateRequestAsync(It.IsAny<HttpContext>()))
            .ThrowsAsync(new AntiforgeryValidationException("invalid"));

        var controller = CreateController(authService, antiforgery);

        var result = await controller.LogoutPost();

        Assert.IsType<BadRequestResult>(result);
        Assert.False(authService.SignedOut);
    }

    [Fact]
    public async Task ConnectController_PostLogout_WithAntiforgery_Redirects()
    {
        var authService = new FakeAuthenticationService();
        var antiforgery = new Mock<IAntiforgery>();
        antiforgery
            .Setup(a => a.ValidateRequestAsync(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        var controller = CreateController(authService, antiforgery);

        var result = await controller.LogoutPost();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/Account/Login", redirect.Url);
        Assert.True(authService.SignedOut);
    }

    [Fact]
    public async Task AccountLogoutEndpoint_InvalidReturnUrl_FallsBackToLogin()
    {
        var authService = new FakeAuthenticationService();
        var context = CreateHttpContext(authService);
        var antiforgery = SuccessfulAntiforgery();

        var result = await AccountLogoutEndpoint.HandleAsync(context, antiforgery.Object, "https://evil.example");

        var redirect = Assert.IsType<RedirectHttpResult>(result);
        Assert.Equal("/Account/Login", redirect.Url);
        Assert.True(authService.SignedOut);
    }

    [Fact]
    public async Task AccountLogoutEndpoint_SafeReturnUrl_IsHonored()
    {
        var authService = new FakeAuthenticationService();
        var context = CreateHttpContext(authService);
        var antiforgery = SuccessfulAntiforgery();

        var result = await AccountLogoutEndpoint.HandleAsync(context, antiforgery.Object, "/admin");

        var redirect = Assert.IsType<RedirectHttpResult>(result);
        Assert.Equal("/admin", redirect.Url);
        Assert.True(authService.SignedOut);
    }

    private static Mock<IAntiforgery> SuccessfulAntiforgery()
    {
        var antiforgery = new Mock<IAntiforgery>();
        antiforgery
            .Setup(a => a.ValidateRequestAsync(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);
        return antiforgery;
    }

    private static ConnectController CreateController(FakeAuthenticationService authService, Mock<IAntiforgery>? antiforgery = null)
    {
        var antiforgeryInstance = antiforgery ?? SuccessfulAntiforgery();
        var controller = new ConnectController(Mock.Of<IAuthFlowService>(), antiforgeryInstance.Object, NullLogger<ConnectController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    RequestServices = BuildServices(authService)
                }
            }
        };

        return controller;
    }

    private static HttpContext CreateHttpContext(FakeAuthenticationService authService)
        => new DefaultHttpContext
        {
            RequestServices = BuildServices(authService)
        };

    private static IServiceProvider BuildServices(FakeAuthenticationService authService)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IAuthenticationService>(authService);
        return services.BuildServiceProvider();
    }

    private sealed class FakeAuthenticationService : IAuthenticationService
    {
        public bool SignedOut { get; private set; }

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
            => Task.FromResult(AuthenticateResult.NoResult());

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            SignedOut = true;
            return Task.CompletedTask;
        }
    }
}
