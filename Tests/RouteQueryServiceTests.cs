using System;
using System.Linq;
using System.Threading.Tasks;
using Auth.Web.Data;
using Auth.Web.Domain.Entities;
using Auth.Web.Services.Implementations.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Auth.Web.Tests
{
    public class RouteQueryServiceTests
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
        public async Task GetUserRoutesAsync_Returns_Empty_When_No_Areas()
        {
            using var db = CreateDb(Guid.NewGuid().ToString());
            var svc = new RouteQueryService(CreateScopeFactory(db));
            var list = await svc.GetUserRoutesAsync("no-user");
            Assert.Empty(list);
        }

        [Fact]
        public async Task GetUserRoutesAsync_Returns_Routes_With_AreaNames()
        {
            var dbName = Guid.NewGuid().ToString();
            using var db = CreateDb(dbName);
            var area = new Area { Name = "IT" };
            db.Areas.Add(area);
            var userArea = new UserArea { UserId = "u1", AreaId = area.Id };
            db.UserAreas.Add(userArea);
            var route = new AreaRoute { AreaId = area.Id, ClientId = "cli", ReturnUrl = "https://app/", Priority = 1, IsActive = true };
            db.AreaRoutes.Add(route);
            await db.SaveChangesAsync();

            var svc = new RouteQueryService(CreateScopeFactory(db));
            var res = await svc.GetUserRoutesAsync("u1");
            Assert.Single(res);
            var r = res.Single();
            Assert.Equal(area.Id, r.AreaId);
            Assert.Equal("IT", r.AreaName);
            Assert.Equal("https://app/", r.ReturnUrl);
        }
    }
}
