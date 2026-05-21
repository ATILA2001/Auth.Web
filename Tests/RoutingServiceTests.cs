using System.Threading.Tasks;
using Auth.Web.Data;
using Auth.Web.Data.Entities;
using Auth.Web.Repositories.Implementations.Routing;
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

        var clientService = new Mock<Auth.Web.Services.Abstractions.Clients.IClientService>();
        var svc = new Auth.Web.Services.Implementations.Routing.RoutingService(new RoutingRepository(CreateDb("r1")), um.Object, new Mock<Microsoft.Extensions.Logging.ILogger<Auth.Web.Services.Implementations.Routing.RoutingService>>().Object, clientService.Object);
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
        var client = new ApplicationClient
        {
            ClientId = "cli",
            Audience = "aud",
            AllowedReturnUrlsJson = "[\"https://app/\"]",
            DefaultLandingPage = "/admin-home"
        };
        db.ApplicationClients.Add(client);
        db.UserAreas.Add(new UserArea { UserId = "u2", AreaId = area.Id });
        var route = new AreaRoute { AreaId = area.Id, ClientId = client.Id, IsActive = true, Priority = 1 };
        db.AreaRoutes.Add(route);
        await db.SaveChangesAsync();

        var um = new Mock<UserManager<ApplicationUser>>(new Mock<IUserStore<ApplicationUser>>().Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var user = new ApplicationUser { Id = "u2", UserName = "user@corp" };
        um.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);
        um.Setup(x => x.IsInRoleAsync(user, "Admin")).ReturnsAsync(false);

        var svc2 = new Auth.Web.Services.Implementations.Routing.RoutingService(new RoutingRepository(db), um.Object, new Mock<Microsoft.Extensions.Logging.ILogger<Auth.Web.Services.Implementations.Routing.RoutingService>>().Object, new Auth.Web.Services.Implementations.Clients.ClientService(new Auth.Web.Repositories.Implementations.Clients.ClientRepository(db)));
        var res = await svc2.ResolveForUserAsync(user.Id);
        Assert.NotNull(res);
        Assert.Equal("cli", res!.Value.ClientId);
        Assert.Equal("https://app/admin-home", res.Value.ReturnUrl);
    }
}
