using Auth.Web.Services.Implementations.Admin;
using Auth.Web.Data;
using Auth.Web.Data.Entities;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Repositories.Abstractions.Admin;
using Auth.Web.Repositories.Implementations.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Xunit;

namespace Auth.Web.Tests.Admin;

public class AreaAdminServiceTests
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

    [Fact]
    public async Task CreateAreaAsync_Creates_New_Area()
    {
        var factory = CreateFactory();
        await using var db = factory.CreateDbContext();
        IAreaAdminRepository repo = new AreaAdminRepository(factory);
        IAdminAreaService svc = new AreaAdminService(repo);
        var id = await svc.CreateAreaAsync("Ventas");
        Assert.True(id > 0);

        await using var verify = factory.CreateDbContext();
        Assert.Equal("Ventas", verify.Areas.Single().Name);
    }

    [Fact]
    public async Task GetAreasAsync_Returns_UserCount()
    {
        var factory = CreateFactory();
        await using (var seed = factory.CreateDbContext())
        {
            var area = new Area { Name = "IT" };
            seed.Areas.Add(area);
            var user = new ApplicationUser { Id = "u1", UserName = "user1" };
            seed.Users.Add(user);
            seed.UserAreas.Add(new UserArea { UserId = user.Id, AreaId = area.Id });
            await seed.SaveChangesAsync();
        }

        IAreaAdminRepository repo = new AreaAdminRepository(factory);
        IAdminAreaService svc = new AreaAdminService(repo);
        var list = await svc.GetAreasAsync();
        var dto = list.Single(a => a.Name == "IT");
        Assert.Equal(1, dto.UserCount);
    }
}
