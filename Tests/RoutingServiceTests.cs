using System.Threading.Tasks;
using Auth.Web.Data;
using Auth.Web.Domain.Entities;
using Auth.Web.Services.Implementations.Routing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Auth.Web.Tests;

public class RoutingServiceTests
{
    private static AuthDbContext CreateDb(string name)
    {
        var opts = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new AuthDbContext(opts);
    }

    private static IServiceScopeFactory CreateScopeFactory(AuthDbContext db)
    {
        var sp = new ServiceCollection().AddScoped(_ => db).BuildServiceProvider();
        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(() =>
        {
            var scope = new Mock<IServiceScope>();
            scope.Setup(s => s.ServiceProvider).Returns(sp);
            return scope.Object;
        });
        return scopeFactory.Object;
    }

    [Fact]
    public async Task ResolveForUserAsync_Returns_Null_For_Admin_User()
    {
        var um = new Mock<UserManager<ApplicationUser>>(new Mock<IUserStore<ApplicationUser>>().Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var user = new ApplicationUser { Id = "u1", UserName = "admin@corp" };
        um.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);
        um.Setup(x => x.IsInRoleAsync(user, "Admin")).ReturnsAsync(true);

        var svc = new RoutingService(CreateScopeFactory(CreateDb("r1")), um.Object, new Mock<Microsoft.Extensions.Logging.ILogger<RoutingService>>().Object);
        var res = await svc.ResolveForUserAsync(user.Id);
        Assert.Null(res);
    }

    [Fact]
    public async Task ResolveForUserAsync_Returns_Rule_For_User_With_Area()
    {
        var dbName = "r2" + System.Guid.NewGuid();
        using var db = CreateDb(dbName);
        var area = new Area { Name = "IT" };
        db.Areas.Add(area);
        db.UserAreas.Add(new UserArea { UserId = "u2", AreaId = area.Id });
        var route = new AreaRoute { AreaId = area.Id, ClientId = "cli", ReturnUrl = "https://app/", IsActive = true, Priority = 1 };
        db.AreaRoutes.Add(route);
        await db.SaveChangesAsync();

        var um = new Mock<UserManager<ApplicationUser>>(new Mock<IUserStore<ApplicationUser>>().Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var user = new ApplicationUser { Id = "u2", UserName = "user@corp" };
        um.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);
        um.Setup(x => x.IsInRoleAsync(user, "Admin")).ReturnsAsync(false);

        var svc = new RoutingService(CreateScopeFactory(db), um.Object, new Mock<Microsoft.Extensions.Logging.ILogger<RoutingService>>().Object);
        var res = await svc.ResolveForUserAsync(user.Id);
        Assert.NotNull(res);
        Assert.Equal("cli", res!.Value.ClientId);
        Assert.Equal("https://app/", res.Value.ReturnUrl);
    }
}
