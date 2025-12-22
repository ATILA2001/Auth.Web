using Auth.Web.Services.Implementations.Admin;
using Auth.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Repositories.Abstractions.Admin;
using Auth.Web.Repositories.Implementations.Admin;
using Xunit;
using Moq;

namespace Auth.Web.Tests.Admin;

public class RoleAdminServiceTests
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

    private static Mock<RoleManager<IdentityRole>> CreateRoleManagerMock()
    {
        var store = new Mock<IRoleStore<IdentityRole>>();
        return new Mock<RoleManager<IdentityRole>>(store.Object, null!, null!, null!, null!);
    }

    [Fact]
    public async Task CreateRoleAsync_Creates_Role()
    {
        var factory = CreateFactory();
        await using var db = factory.CreateDbContext();
        var rmMock = CreateRoleManagerMock();
        IdentityRole? created = null;
        rmMock.Setup(r => r.RoleExistsAsync("NuevoRol")).ReturnsAsync(false);
        rmMock.Setup(r => r.CreateAsync(It.Is<IdentityRole>(x => x.Name == "NuevoRol")))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<IdentityRole>(r => { created = r; r.Id = "role-123"; db.Roles.Add(r); db.SaveChanges(); });
        rmMock.Setup(r => r.FindByNameAsync("NuevoRol")).ReturnsAsync(() => created!);

        IRoleAdminRepository repo = new RoleAdminRepository(factory);
        IAdminRoleService svc = new RoleAdminService(rmMock.Object, repo);
        var id = await svc.CreateRoleAsync("NuevoRol");

        rmMock.Verify(r => r.CreateAsync(It.Is<IdentityRole>(x => x.Name == "NuevoRol")), Times.Once);
        Assert.Equal("role-123", id);

        await using var verify = factory.CreateDbContext();
        Assert.Contains(verify.Roles, r => r.Name == "NuevoRol");
    }

    [Fact]
    public async Task GetRolesAsync_Returns_UserCount()
    {
        var factory = CreateFactory();
        await using (var seed = factory.CreateDbContext())
        {
            var role = new IdentityRole("User") { Id = "role-user" };
            seed.Roles.Add(role);
            seed.UserRoles.Add(new IdentityUserRole<string> { RoleId = role.Id, UserId = "u1" });
            await seed.SaveChangesAsync();
        }

        var rmMock = CreateRoleManagerMock();
        IRoleAdminRepository repo = new RoleAdminRepository(factory);
        IAdminRoleService svc = new RoleAdminService(rmMock.Object, repo);
        var roles = await svc.GetRolesAsync();
        var dto = roles.Single(r => r.Name == "User");
        Assert.Equal(1, dto.UserCount);
    }
}
