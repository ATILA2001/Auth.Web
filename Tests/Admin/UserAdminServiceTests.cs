using Auth.Web.Services.Implementations.Admin;
using Auth.Web.Data;
using Auth.Web.Data.Entities;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Services.Abstractions.Permissions;
using Auth.Web.Repositories.Abstractions.Admin;
using Auth.Web.Repositories.Implementations.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Xunit;
using Moq;

namespace Auth.Web.Tests.Admin;

public class UserAdminServiceTests
{
    private static IDbContextFactory<AuthDbContext> CreateFactory()
    {
        var opts = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestDbFactory(opts);
    }

    private sealed class TestDbFactory : IDbContextFactory<AuthDbContext>
    {
        private readonly DbContextOptions<AuthDbContext> _options;
        public TestDbFactory(DbContextOptions<AuthDbContext> options) => _options = options;
        public AuthDbContext CreateDbContext() => new(_options);
        public ValueTask<AuthDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => new(new AuthDbContext(_options));
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<ApplicationUser>> ();
        return new Mock<UserManager<ApplicationUser>> (store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    [Fact]
    public async Task GetUsersAsync_Returns_Roles_And_Areas()
    {
        var factory = CreateFactory();
        int areaId;
        await using (var seed = factory.CreateDbContext())
        {
            var user = new ApplicationUser { Id = "u1", UserName = "user1", Email = "user1@test" };
            seed.Users.Add(user);
            var role = new IdentityRole("Admin") { Id = "r1" };
            seed.Roles.Add(role);
            await seed.SaveChangesAsync();
            seed.UserRoles.Add(new IdentityUserRole<string> { RoleId = role.Id, UserId = user.Id });
            var area = new Area { Name = "IT" };
            seed.Areas.Add(area);
            await seed.SaveChangesAsync();
            areaId = area.Id;
            seed.UserAreas.Add(new UserArea { UserId = user.Id, AreaId = areaId });
            await seed.SaveChangesAsync();
        }

        var umMock = CreateUserManagerMock();
        var userRef = new ApplicationUser { Id = "u1", UserName = "user1", Email = "user1@test" };
        umMock.Setup(m => m.GetRolesAsync(It.Is<ApplicationUser>(u => u.Id == "u1"))).ReturnsAsync(new List<string> { "Admin" });
        umMock.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(userRef);

        IUserAdminRepository repo = new UserAdminRepository(factory);
        var auditMock = new Mock<IPermissionAuditService>();
        IAdminUserService svc = new UserAdminService(repo, umMock.Object, auditMock.Object);
        var users = await svc.GetUsersAsync();
        var dto = users.Single();
        Assert.Contains("Admin", dto.Roles);
        Assert.Contains("IT", dto.Areas);
        Assert.Contains(areaId, dto.AreaIds);
    }

    [Fact]
    public async Task UpdateUserRolesAndAreasAsync_Updates_Roles_And_Areas()
    {
        var factory = CreateFactory();
        int area1Id;
        int area2Id;
        await using (var seed = factory.CreateDbContext())
        {
            var user = new ApplicationUser { Id = "user-1", UserName = "user1", Email = "user1@test" };
            seed.Users.Add(user);
            var area1 = new Area { Name = "Area1" };
            var area2 = new Area { Name = "Area2" };
            seed.Areas.AddRange(area1, area2);
            await seed.SaveChangesAsync();
            area1Id = area1.Id;
            area2Id = area2.Id;
            seed.UserAreas.Add(new UserArea { UserId = user.Id, AreaId = area1Id });
            await seed.SaveChangesAsync();
        }

        var umMock = CreateUserManagerMock();
        var userRef = new ApplicationUser { Id = "user-1", UserName = "user1", Email = "user1@test" };
        umMock.Setup(m => m.FindByIdAsync("user-1")).ReturnsAsync(userRef);
        umMock.Setup(m => m.GetRolesAsync(It.Is<ApplicationUser>(u => u.Id == "user-1"))).ReturnsAsync(new List<string> { "RoleA" });
        umMock.Setup(m => m.AddToRolesAsync(It.Is<ApplicationUser>(u => u.Id == "user-1"), It.Is<IEnumerable<string>>(r => r.Single() == "RoleB")))
            .ReturnsAsync(IdentityResult.Success);
        umMock.Setup(m => m.RemoveFromRolesAsync(It.Is<ApplicationUser>(u => u.Id == "user-1"), It.Is<IEnumerable<string>>(r => r.Single() == "RoleA")))
            .ReturnsAsync(IdentityResult.Success);

        IUserAdminRepository repo = new UserAdminRepository(factory);
        var auditMock = new Mock<IPermissionAuditService>();
        auditMock.Setup(a => a.IncrementUserPermissionVersionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        IAdminUserService svc = new UserAdminService(repo, umMock.Object, auditMock.Object);
        await svc.UpdateUserRolesAndAreasAsync("user-1", new[] { "RoleB" }, new[] { area2Id });

        umMock.Verify(m => m.AddToRolesAsync(It.Is<ApplicationUser>(u => u.Id == "user-1"), It.Is<IEnumerable<string>>(r => r.Single() == "RoleB")), Times.Once);
        umMock.Verify(m => m.RemoveFromRolesAsync(It.Is<ApplicationUser>(u => u.Id == "user-1"), It.Is<IEnumerable<string>>(r => r.Single() == "RoleA")), Times.Once);

        await using var verify = factory.CreateDbContext();
        var userAreas = await verify.UserAreas.Where(ua => ua.UserId == "user-1").ToListAsync();
        Assert.Single(userAreas);
        Assert.Equal(area2Id, userAreas.Single().AreaId);
        Assert.DoesNotContain(userAreas, ua => ua.AreaId == area1Id);
    }
}
