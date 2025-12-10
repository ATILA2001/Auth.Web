using Auth.Web.Services.Implementations.Admin;
using Auth.Web.Data;
using Auth.Web.Data.Entities;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Repositories.Abstractions.Admin;
using Auth.Web.Repositories.Implementations.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Moq;

namespace Auth.Web.Tests.Admin;

public class AreaAdminServiceTests
{
    private static AuthDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AuthDbContext(opts);
    }

    [Fact]
    public async Task CreateAreaAsync_Creates_New_Area()
    {
        using var db = CreateDb();
        IAreaAdminRepository repo = new AreaAdminRepository(db);
        IAdminAreaService svc = new AreaAdminService(repo);
        var id = await svc.CreateAreaAsync("Ventas");
        Assert.True(id > 0);
        Assert.Equal("Ventas", db.Areas.Single().Name);
    }

    [Fact]
    public async Task GetAreasAsync_Returns_UserCount()
    {
        using var db = CreateDb();
        var area = new Area { Name = "IT" };
        db.Areas.Add(area);
        var user = new ApplicationUser { Id = "u1", UserName = "user1" };
        db.Users.Add(user);
        db.UserAreas.Add(new UserArea { UserId = user.Id, AreaId = area.Id });
        await db.SaveChangesAsync();

        IAreaAdminRepository repo = new AreaAdminRepository(db);
        IAdminAreaService svc = new AreaAdminService(repo);
        var list = await svc.GetAreasAsync();
        var dto = list.Single(a => a.Name == "IT");
        Assert.Equal(1, dto.UserCount);
    }
}
