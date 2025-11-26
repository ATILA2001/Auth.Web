using Auth.Web.Infrastructure.Admin;
using Auth.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Auth.Web.Application.Admin.Abstractions;
using Xunit;
using Moq;

namespace Auth.Web.Tests.Admin;

public class RoleAdminServiceTests
{
    private static AuthDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AuthDbContext(opts);
    }

    private static Mock<RoleManager<IdentityRole>> CreateRoleManagerMock()
    {
        var store = new Mock<IRoleStore<IdentityRole>>();
        return new Mock<RoleManager<IdentityRole>>(store.Object, null!, null!, null!, null!);
    }

    [Fact]
    public async Task CreateRoleAsync_Creates_Role()
    {
        using var db = CreateDb();
        var rmMock = CreateRoleManagerMock();
        IdentityRole? created = null;
        rmMock.Setup(r => r.RoleExistsAsync("NuevoRol")).ReturnsAsync(false);
        rmMock.Setup(r => r.CreateAsync(It.Is<IdentityRole>(x => x.Name == "NuevoRol")))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<IdentityRole>(r => { created = r; r.Id = "role-123"; db.Roles.Add(r); db.SaveChanges(); });
        rmMock.Setup(r => r.FindByNameAsync("NuevoRol")).ReturnsAsync(() => created!);

        IAdminRoleService svc = new RoleAdminService(rmMock.Object, db);
        var id = await svc.CreateRoleAsync("NuevoRol");

        rmMock.Verify(r => r.CreateAsync(It.Is<IdentityRole>(x => x.Name == "NuevoRol")), Times.Once);
        Assert.Equal("role-123", id);
        Assert.Contains(db.Roles, r => r.Name == "NuevoRol");
    }

    [Fact]
    public async Task GetRolesAsync_Returns_UserCount()
    {
        using var db = CreateDb();
        var role = new IdentityRole("User") { Id = "role-user" };
        db.Roles.Add(role);
        db.UserRoles.Add(new IdentityUserRole<string> { RoleId = role.Id, UserId = "u1" });
        await db.SaveChangesAsync();

        var rmMock = CreateRoleManagerMock();
        IAdminRoleService svc = new RoleAdminService(rmMock.Object, db);
        var roles = await svc.GetRolesAsync();
        var dto = roles.Single(r => r.Name == "User");
        Assert.Equal(1, dto.UserCount);
    }
}
