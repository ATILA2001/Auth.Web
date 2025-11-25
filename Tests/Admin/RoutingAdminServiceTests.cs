using Auth.Web.Infrastructure.Admin;
using Auth.Web.Data;
using Auth.Web.Domain.Entities;
using Auth.Web.Application.Admin.Abstractions;
using Auth.Web.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore.Storage;

namespace Auth.Web.Tests.Admin;

public class RoutingAdminServiceTests
{
    [Fact]
    public async Task CreateRouteAsync_Inserts_When_ReturnUrl_Allowed()
    {
        var clientSvc = new Mock<IClientService>();
        var dbName = Guid.NewGuid().ToString();
        var root = new InMemoryDatabaseRoot();
        var services = new ServiceCollection();
        services.AddDbContext<AuthDbContext>(opts => opts.UseInMemoryDatabase(dbName, root));
        services.AddScoped<IClientService>(_ => clientSvc.Object);
        services.AddScoped<IAdminRoutingService, RoutingAdminService>();
        var provider = services.BuildServiceProvider();

        Area area;
        ApplicationClient client;
        using (var seedScope = provider.CreateScope())
        {
            var dbSeed = seedScope.ServiceProvider.GetRequiredService<AuthDbContext>();
            area = new Area { Name = "IT" };
            client = new ApplicationClient { ClientId = "cli1", Audience = "aud", AllowedReturnUrlsJson = System.Text.Json.JsonSerializer.Serialize(new[] { "https://app/x" }) };
            dbSeed.Areas.Add(area);
            dbSeed.ApplicationClients.Add(client);
            await dbSeed.SaveChangesAsync();
        }

        clientSvc.Setup(x => x.IsReturnUrlAllowed(It.Is<ApplicationClient>(c => c.ClientId == "cli1"), "https://app/x")).Returns(true);

        var admin = provider.GetRequiredService<IAdminRoutingService>();
        var id = await admin.CreateRouteAsync(area.Id, client.Id, "https://app/x", 1, true);
        Assert.True(id > 0);
        var routes = await admin.GetRoutesAsync();
        Assert.Equal(1, routes.Count);
        Assert.Contains(routes, r => r.ReturnUrl == "https://app/x");
    }

    [Fact]
    public async Task CreateRouteAsync_Throws_When_ReturnUrl_Not_Allowed()
    {
        var clientSvc = new Mock<IClientService>();
        var dbName = Guid.NewGuid().ToString();
        var root = new InMemoryDatabaseRoot();
        var services = new ServiceCollection();
        services.AddDbContext<AuthDbContext>(opts => opts.UseInMemoryDatabase(dbName, root));
        services.AddScoped<IClientService>(_ => clientSvc.Object);
        services.AddScoped<IAdminRoutingService, RoutingAdminService>();
        var provider = services.BuildServiceProvider();

        Area area;
        ApplicationClient client;
        using (var seedScope = provider.CreateScope())
        {
            var dbSeed = seedScope.ServiceProvider.GetRequiredService<AuthDbContext>();
            area = new Area { Name = "IT" };
            client = new ApplicationClient { ClientId = "cli2", Audience = "aud", AllowedReturnUrlsJson = System.Text.Json.JsonSerializer.Serialize(new[] { "https://app/y" }) };
            dbSeed.Areas.Add(area);
            dbSeed.ApplicationClients.Add(client);
            await dbSeed.SaveChangesAsync();
        }

        clientSvc.Setup(x => x.IsReturnUrlAllowed(It.Is<ApplicationClient>(c => c.ClientId == "cli2"), "https://bad/url")).Returns(false);

        var admin = provider.GetRequiredService<IAdminRoutingService>();
        await Assert.ThrowsAsync<InvalidOperationException>(() => admin.CreateRouteAsync(area.Id, client.Id, "https://bad/url", 1, true));
    }
}
