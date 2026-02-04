using Auth.Web.Services.Implementations.Admin;
using Auth.Web.Data;
using Auth.Web.Data.Entities;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Services.Abstractions.Clients;
using Auth.Web.Repositories.Abstractions;
using Auth.Web.Repositories.Abstractions.Admin;
using Auth.Web.Repositories.Implementations;
using Auth.Web.Repositories.Implementations.Admin;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore.Storage;

namespace Auth.Web.Tests.Admin;

public class RoutingAdminServiceTests
{
    [Fact]
    public async Task CreateRouteAsync_Inserts_With_Client_And_Area()
    {
        var clientSvc = new Mock<IClientService>();
        var dbName = Guid.NewGuid().ToString();
        var root = new InMemoryDatabaseRoot();
        var services = new ServiceCollection();
        services.AddDbContext<AuthDbContext>(opts => opts.UseInMemoryDatabase(dbName, root));
        services.AddDbContextFactory<AuthDbContext>(opts => opts.UseInMemoryDatabase(dbName, root));
        services.AddScoped<IClientService>(_ => clientSvc.Object);
        services.AddScoped<IClientAdminRepository, ClientAdminRepository>();
        services.AddScoped<IAreaAdminRepository, AreaAdminRepository>();
        services.AddScoped<IAreaRepository, AreaRepository>();
        services.AddScoped<IRoutingAdminRepository, RoutingAdminRepository>();
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

        clientSvc.Setup(x => x.GetAsync("cli1")).ReturnsAsync(client);

        var admin = provider.GetRequiredService<IAdminRoutingService>();
        var id = await admin.CreateRouteAsync(area.Id, client.Id, 1, true);
        Assert.True(id > 0);
        var routes = await admin.GetRoutesAsync();
        Assert.Single(routes);
        Assert.Contains(routes, r => r.AreaName == "IT");
    }
}
