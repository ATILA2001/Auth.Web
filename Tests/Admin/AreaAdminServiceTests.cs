using Auth.Web.Services.Admin;
using Auth.Web.Data;
using Auth.Web.Domain.Entities;
using Auth.Web.Application.Admin.Abstractions;
using Microsoft.EntityFrameworkCore;
using Xunit;

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
        IAdminAreaService svc = new AreaAdminService(db);
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

        IAdminAreaService svc = new AreaAdminService(db);
        var list = await svc.GetAreasAsync();
        var dto = list.Single(a => a.Name == "IT");
        Assert.Equal(1, dto.UserCount);
    }
}
