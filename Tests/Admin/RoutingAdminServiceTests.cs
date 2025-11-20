using Auth.Web.Infrastructure.Admin;
using Auth.Web.Data;
using Auth.Web.Domain.Entities;
using Auth.Web.Application.Admin.Abstractions;
using Auth.Web.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Auth.Web.Tests.Admin;

public class RoutingAdminServiceTests
{
    private static AuthDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AuthDbContext(opts);
    }

    [Fact]
    public async Task CreateRouteAsync_Inserts_When_ReturnUrl_Allowed()
    {
        using var db = CreateDb();
        var area = new Area { Name = "IT" };
        var client = new ApplicationClient { ClientId = "cli1", Audience = "aud", AllowedReturnUrlsJson = System.Text.Json.JsonSerializer.Serialize(new[] { "https://app/x" }) };
        db.Areas.Add(area);
        db.ApplicationClients.Add(client);
        await db.SaveChangesAsync();
        var clientSvc = new Mock<IClientService>();
        clientSvc.Setup(x => x.IsReturnUrlAllowed(client, "https://app/x")).Returns(true);
        IAdminRoutingService admin = new RoutingAdminService(db, clientSvc.Object);
        var id = await admin.CreateRouteAsync(area.Id, client.Id, "https://app/x", 1, true);
        Assert.True(id > 0);
        Assert.Equal(1, db.AreaRoutes.Count());
    }

    [Fact]
    public async Task CreateRouteAsync_Throws_When_ReturnUrl_Not_Allowed()
    {
        using var db = CreateDb();
        var area = new Area { Name = "IT" };
        var client = new ApplicationClient { ClientId = "cli2", Audience = "aud", AllowedReturnUrlsJson = System.Text.Json.JsonSerializer.Serialize(new[] { "https://app/y" }) };
        db.Areas.Add(area);
        db.ApplicationClients.Add(client);
        await db.SaveChangesAsync();
        var clientSvc = new Mock<IClientService>();
        clientSvc.Setup(x => x.IsReturnUrlAllowed(client, "https://bad/url")).Returns(false);
        IAdminRoutingService admin = new RoutingAdminService(db, clientSvc.Object);
        await Assert.ThrowsAsync<InvalidOperationException>(() => admin.CreateRouteAsync(area.Id, client.Id, "https://bad/url", 1, true));
    }
}
