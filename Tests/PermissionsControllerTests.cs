using System.Threading.Tasks;
using Auth.Web.Application.Permissions.Dtos;
using Auth.Web.Controllers;
using Auth.Web.Services.Abstractions.Permissions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Auth.Web.Tests;

public class PermissionsControllerTests
{
    private static PermissionsController CreateController(IPermissionService service, bool setUser = false)
    {
        var controller = new PermissionsController(service);

        if (setUser)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.User = new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(
                    new[] { new System.Security.Claims.Claim("sub", "testuser") },
                    "TestAuth"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        return controller;
    }

    [Fact]
    public async Task GetAsync_ForExistingUser_Returns_PermissionsDto()
    {
        var svc = new Mock<IPermissionService>();
        svc.Setup(s => s.GetAsync("alice", null, null, null))
            .ReturnsAsync(new UserPermissionsDto
            {
                AreaIds = new System.Collections.Generic.List<int> { 1, 2 },
                Pages = new System.Collections.Generic.List<PagePermissionDto>
                {
                    new PagePermissionDto { Url = "/home", Actions = new System.Collections.Generic.List<string>{ "View" } }
                },
                Version = 5
            });

        var controller = CreateController(svc.Object);
        var result = await controller.GetAsync("alice");

        Assert.NotNull(result);
        Assert.Equal(5, result.Version);
        Assert.Contains(1, result.AreaIds);
        Assert.Single(result.Pages);
        Assert.Equal("/home", result.Pages[0].Url);
    }

    [Fact]
    public async Task GetAsync_Delegates_To_Service_With_Provided_UserName()
    {
        var svc = new Mock<IPermissionService>();
        svc.Setup(s => s.GetAsync(It.IsAny<string>(), null, null, null)).ReturnsAsync(new UserPermissionsDto());

        var controller = CreateController(svc.Object);
        var userName = "bob";
        await controller.GetAsync(userName);

        svc.Verify(s => s.GetAsync(userName, null, null, null), Times.Once);
    }

    [Fact]
    public async Task GetAsync_Returns_Empty_Permissions_When_User_Not_Found()
    {
        var svc = new Mock<IPermissionService>();
        svc.Setup(s => s.GetAsync("nonexistent", null, null, null))
            .ReturnsAsync(new UserPermissionsDto
            {
                AreaIds = new System.Collections.Generic.List<int>(),
                Pages = new System.Collections.Generic.List<PagePermissionDto>(),
                Version = 0
            });

        var controller = CreateController(svc.Object);
        var result = await controller.GetAsync("nonexistent");

        Assert.NotNull(result);
        Assert.Empty(result.AreaIds);
        Assert.Empty(result.Pages);
        Assert.Equal(0, result.Version);
    }

    [Fact]
    public async Task GetAsync_Returns_Multiple_Pages_With_Actions()
    {
        var svc = new Mock<IPermissionService>();
        svc.Setup(s => s.GetAsync("user", null, null, null))
            .ReturnsAsync(new UserPermissionsDto
            {
                AreaIds = new System.Collections.Generic.List<int> { 1 },
                Pages = new System.Collections.Generic.List<PagePermissionDto>
                {
                    new() { Url = "/dashboard", Actions = new System.Collections.Generic.List<string> { "Read", "Write" } },
                    new() { Url = "/reports", Actions = new System.Collections.Generic.List<string> { "Read" } }
                },
                Version = 10
            });

        var controller = CreateController(svc.Object);
        var result = await controller.GetAsync("user");

        Assert.Equal(2, result.Pages.Count);
        Assert.Contains(result.Pages, p => p.Url == "/dashboard" && p.Actions.Count == 2);
        Assert.Contains(result.Pages, p => p.Url == "/reports" && p.Actions.Count == 1);
    }

    [Fact]
    public async Task GetAsync_Preserves_Version_Number()
    {
        var svc = new Mock<IPermissionService>();
        svc.Setup(s => s.GetAsync(It.IsAny<string>(), null, null, null))
            .ReturnsAsync(new UserPermissionsDto { Version = 42 });

        var controller = CreateController(svc.Object);
        var result = await controller.GetAsync("anyuser");

        Assert.Equal(42, result.Version);
    }

    [Fact]
    public async Task GetAsync_Handles_Special_Characters_In_UserName()
    {
        var svc = new Mock<IPermissionService>();
        svc.Setup(s => s.GetAsync("user@domain.com", null, null, null))
            .ReturnsAsync(new UserPermissionsDto { Version = 1 });

        var controller = CreateController(svc.Object);
        await controller.GetAsync("user@domain.com");

        svc.Verify(s => s.GetAsync("user@domain.com", null, null, null), Times.Once);
    }

    [Fact]
    public void Controller_Has_Authorize_Attribute()
    {
        var controllerType = typeof(PermissionsController);
        var authorizeAttribute = controllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false);

        Assert.NotEmpty(authorizeAttribute);
    }

    [Fact]
    public void Controller_Has_ApiController_Attribute()
    {
        var controllerType = typeof(PermissionsController);
        var apiControllerAttribute = controllerType.GetCustomAttributes(typeof(ApiControllerAttribute), false);

        Assert.NotEmpty(apiControllerAttribute);
    }
}
