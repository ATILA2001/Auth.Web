using Auth.Web.Services.Implementations.Admin;
using Auth.Web.Data;
using Auth.Web.Domain.Entities;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Repositories.Abstractions.Admin;
using Auth.Web.Repositories.Implementations.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Moq;

namespace Auth.Web.Tests.Admin;

public class UserAdminServiceTests
{
    private static AuthDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AuthDbContext(opts);
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<ApplicationUser>> ();
        return new Mock<UserManager<ApplicationUser>> (store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    [Fact]
    public async Task GetUsersAsync_Returns_Roles_And_Areas()
    {
        using var db = CreateDb();
        var user = new ApplicationUser { Id = "u1", UserName = "user1", Email = "user1@test" };
        db.Users.Add(user);
        var role = new IdentityRole("Admin") { Id = "r1" };
        db.Roles.Add(role);
        await db.SaveChangesAsync();
        db.UserRoles.Add(new IdentityUserRole<string> { RoleId = role.Id, UserId = user.Id });
        var area = new Area { Name = "IT" };
        db.Areas.Add(area);
        await db.SaveChangesAsync();
        db.UserAreas.Add(new UserArea { UserId = user.Id, AreaId = area.Id });
        await db.SaveChangesAsync();

        var umMock = CreateUserManagerMock();
        umMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin" });
        umMock.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);

        IUserAdminRepository repo = new UserAdminRepository(db);
        IAdminUserService svc = new UserAdminService(repo, umMock.Object);
        var users = await svc.GetUsersAsync();
        var dto = users.Single();
        Assert.Contains("Admin", dto.Roles);
        Assert.Contains("IT", dto.Areas);
        Assert.Contains(area.Id, dto.AreaIds);
    }

    [Fact]
    public async Task UpdateUserRolesAndAreasAsync_Updates_Roles_And_Areas()
    {
        using var db = CreateDb();
        var user = new ApplicationUser { Id = "user-1", UserName = "user1", Email = "user1@test" };
        db.Users.Add(user);
        var area1 = new Area { Name = "Area1" };
        var area2 = new Area { Name = "Area2" };
        db.Areas.AddRange(area1, area2);
        await db.SaveChangesAsync();
        db.UserAreas.Add(new UserArea { UserId = user.Id, AreaId = area1.Id });
        await db.SaveChangesAsync();

        var umMock = CreateUserManagerMock();
        umMock.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        umMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "RoleA" });
        umMock.Setup(m => m.AddToRolesAsync(user, It.Is<IEnumerable<string>> (r => r.Single() == "RoleB")))
            .ReturnsAsync(IdentityResult.Success);
        umMock.Setup(m => m.RemoveFromRolesAsync(user, It.Is<IEnumerable<string>> (r => r.Single() == "RoleA")))
            .ReturnsAsync(IdentityResult.Success);

        IUserAdminRepository repo = new UserAdminRepository(db);
        IAdminUserService svc = new UserAdminService(repo, umMock.Object);
        await svc.UpdateUserRolesAndAreasAsync(user.Id, new[] { "RoleB" }, new[] { area2.Id });

        umMock.Verify(m => m.AddToRolesAsync(user, It.Is<IEnumerable<string>> (r => r.Single() == "RoleB")), Times.Once);
        umMock.Verify(m => m.RemoveFromRolesAsync(user, It.Is<IEnumerable<string>> (r => r.Single() == "RoleA")), Times.Once);

        var userAreas = await db.UserAreas.Where(ua => ua.UserId == user.Id).ToListAsync();
        Assert.Single(userAreas);
        Assert.Equal(area2.Id, userAreas.Single().AreaId);
        Assert.DoesNotContain(userAreas, ua => ua.AreaId == area1.Id);
    }
}
