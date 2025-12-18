using System.Threading.Tasks;
using Auth.Web.Application.Permissions.Dtos;
using Auth.Web.Controllers;
using Auth.Web.Services.Abstractions.Permissions;
using Moq;
using Xunit;

namespace Auth.Web.Tests;

public class PermissionsControllerTests
{
    [Fact]
    public async Task GetAsync_ForExistingUser_Returns_PermissionsDto()
    {
        var svc = new Mock<IPermissionService>();
        svc.Setup(s => s.GetAsync("alice"))
            .ReturnsAsync(new UserPermissionsDto
            {
                Areas = new System.Collections.Generic.List<int> { 1, 2 },
                Pages = new System.Collections.Generic.List<PagePermissionDto>
                {
                    new PagePermissionDto { Url = "/home", Actions = new System.Collections.Generic.List<string>{ "View" } }
                },
                Version = 5
            });

        var controller = new PermissionsController(svc.Object);
        var result = await controller.GetAsync("alice");

        Assert.NotNull(result);
        Assert.Equal(5, result.Version);
        Assert.Contains(1, result.Areas);
        Assert.Single(result.Pages);
        Assert.Equal("/home", result.Pages[0].Url);
    }

    [Fact]
    public async Task GetAsync_Delegates_To_Service_With_Provided_UserName()
    {
        var svc = new Mock<IPermissionService>();
        svc.Setup(s => s.GetAsync(It.IsAny<string>())).ReturnsAsync(new UserPermissionsDto());

        var controller = new PermissionsController(svc.Object);
        var userName = "bob";
        await controller.GetAsync(userName);

        svc.Verify(s => s.GetAsync(userName), Times.Once);
    }
}
