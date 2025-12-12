using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Auth.Web.Data;
using Auth.Web.Data.Entities;
using Auth.Web.Repositories.Implementations.Permissions;
using Auth.Web.Services.Implementations.Permissions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Auth.Web.Tests;

public class PermissionServiceTests
{
    private static AuthDbContext CreateDb(string name)
    {
        var opts = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new AuthDbContext(opts);
    }

    [Fact]
    public async Task GetAsync_Returns_Empty_When_User_NotFound()
    {
        var db = CreateDb("perm1");
        var um = new Mock<UserManager<ApplicationUser>>(new Mock<IUserStore<ApplicationUser>>().Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var svc = new Auth.Web.Services.Implementations.Permissions.PermissionService(new PermissionRepository(db), um.Object);
        var result = await svc.GetAsync("missing");
        Assert.NotNull(result);
        Assert.Empty(result.Pages);
        Assert.Empty(result.Areas);
    }

    [Fact]
    public async Task GetAsync_Returns_Pages_And_Areas_For_User_With_Roles()
    {
        var dbName = "perm2" + System.Guid.NewGuid();
        using var db = CreateDb(dbName);
        var user = new ApplicationUser { Id = "u1", UserName = "user1" };
        db.Users.Add(user);
        var role = new IdentityRole("Admin") { Id = "r1" };
        db.Roles.Add(role);
        var page = new Page { Name = "Home", Url = "/home" };
        db.Pages.Add(page);
        var action = new ActionPermission { Name = "View" };
        db.ActionPermissions.Add(action);
        await db.SaveChangesAsync();
        db.RolePagePermissions.Add(new RolePagePermission { RoleId = role.Id, PageId = page.Id, ActionPermissionId = action.Id });
        db.UserAreas.Add(new UserArea { UserId = user.Id, AreaId = 5 });
        await db.SaveChangesAsync();

        var um = new Mock<UserManager<ApplicationUser>>(new Mock<IUserStore<ApplicationUser>>().Object, null!, null!, null!, null!, null!, null!, null!, null!);
        um.Setup(x => x.FindByNameAsync(user.UserName)).ReturnsAsync(user);
        um.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin" });

        var svc = new Auth.Web.Services.Implementations.Permissions.PermissionService(new PermissionRepository(db), um.Object);
        var result = await svc.GetAsync(user.UserName);

        Assert.Single(result.Pages);
        Assert.Contains(5, result.Areas);
        Assert.Equal(1, result.Version);
    }
}
