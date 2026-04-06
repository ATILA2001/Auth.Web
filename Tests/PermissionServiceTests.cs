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
        var svc = new PermissionService(new PermissionRepository(db), um.Object);
        var result = await svc.GetAsync("missing");
        Assert.NotNull(result);
        Assert.Empty(result.Pages);
        Assert.Empty(result.AreaIds);
    }

    [Fact]
    public async Task GetAsync_Returns_Empty_Pages_For_Admin_User()
    {
        // Admin bypass: sin importar qué AreaPagePermission exista, Admin recibe vacío
        var dbName = "perm_admin_" + System.Guid.NewGuid();
        using var db = CreateDb(dbName);
        var user = new ApplicationUser { Id = "u1", UserName = "admin@corp" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var um = new Mock<UserManager<ApplicationUser>>(new Mock<IUserStore<ApplicationUser>>().Object, null!, null!, null!, null!, null!, null!, null!, null!);
        um.Setup(x => x.FindByNameAsync(user.UserName!)).ReturnsAsync(user);
        um.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin" });

        var svc = new PermissionService(new PermissionRepository(db), um.Object);
        var result = await svc.GetAsync(user.UserName!);

        Assert.Empty(result.Pages);
        Assert.Empty(result.AreaIds);
    }

    [Fact]
    public async Task GetAsync_Returns_AreaIds_For_User()
    {
        var dbName = "perm_area_" + System.Guid.NewGuid();
        using var db = CreateDb(dbName);
        var user = new ApplicationUser { Id = "u2", UserName = "user@corp" };
        db.Users.Add(user);
        var area = new Area { Name = "Redeterminaciones" };
        db.Areas.Add(area);
        await db.SaveChangesAsync();
        db.UserAreas.Add(new UserArea { UserId = user.Id, AreaId = area.Id });
        await db.SaveChangesAsync();

        var um = new Mock<UserManager<ApplicationUser>>(new Mock<IUserStore<ApplicationUser>>().Object, null!, null!, null!, null!, null!, null!, null!, null!);
        um.Setup(x => x.FindByNameAsync(user.UserName!)).ReturnsAsync(user);
        um.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Usuario" });

        var svc = new PermissionService(new PermissionRepository(db), um.Object);
        var result = await svc.GetAsync(user.UserName!);

        Assert.Contains(area.Id, result.AreaIds);
    }
}
