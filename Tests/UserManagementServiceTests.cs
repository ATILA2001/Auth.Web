using System.Collections.Generic;
using System.Threading.Tasks;
using Auth.Web.Domain.Entities;
using Auth.Web.Infrastructure.Users;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace Auth.Web.Tests;

public class UserManagementServiceTests
{
    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var mgr = new Mock<UserManager<ApplicationUser>>(store.Object,
            null, null, null, null, null, null, null, null);
        return mgr;
    }

    [Fact]
    public async Task FindByNameAsync_ReturnsUser_WhenExists()
    {
        var user = new ApplicationUser { Id = "u1", UserName = "john.doe", Email = "john.doe@corp" };
        var um = CreateUserManagerMock();
        um.Setup(x => x.FindByNameAsync("john.doe")).ReturnsAsync(user);

        var svc = new UserManagementService(um.Object);
        var result = await svc.FindByNameAsync("john.doe");

        Assert.NotNull(result);
        Assert.Equal(user.Id, result!.Id);
        Assert.Equal(user.UserName, result.UserName);
    }

    [Fact]
    public async Task FindByNameAsync_ReturnsNull_WhenNotFound()
    {
        var um = CreateUserManagerMock();
        um.Setup(x => x.FindByNameAsync("missing")).ReturnsAsync((ApplicationUser?)null);

        var svc = new UserManagementService(um.Object);
        var result = await svc.FindByNameAsync("missing");

        Assert.Null(result);
    }

    [Fact]
    public async Task FindByEmailAsync_ReturnsUser_WhenExists()
    {
        var user = new ApplicationUser { Id = "u2", UserName = "jane.doe", Email = "jane.doe@corp" };
        var um = CreateUserManagerMock();
        um.Setup(x => x.FindByEmailAsync("jane.doe@corp")).ReturnsAsync(user);

        var svc = new UserManagementService(um.Object);
        var result = await svc.FindByEmailAsync("jane.doe@corp");

        Assert.NotNull(result);
        Assert.Equal(user.Email, result!.Email);
    }

    [Fact]
    public async Task GetRolesAsync_ReturnsRolesList()
    {
        var user = new ApplicationUser { Id = "u3", UserName = "rol.user", Email = "r.user@corp" };
        var roles = new List<string> { "Admin", "User" };

        var um = CreateUserManagerMock();
        um.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(roles);

        var svc = new UserManagementService(um.Object);
        var result = await svc.GetRolesAsync(user);

        Assert.Equal(2, result.Count);
        Assert.Contains("Admin", result);
        Assert.Contains("User", result);
    }
}
