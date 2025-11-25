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

public class ClientAdminServiceTests
{
    [Fact]
    public async Task CreateClientAsync_Persists_Client_With_Urls()
    {
        var clientSvcMock = new Mock<IClientService>();
        var dbName = Guid.NewGuid().ToString();
        var root = new InMemoryDatabaseRoot();
        var services = new ServiceCollection();
        services.AddDbContext<AuthDbContext>(opts => opts.UseInMemoryDatabase(dbName, root));
        services.AddScoped<IClientService>(_ => clientSvcMock.Object);
        services.AddScoped<IAdminClientService, ClientAdminService>();
        var provider = services.BuildServiceProvider();

        var admin = provider.GetRequiredService<IAdminClientService>();
        var id = await admin.CreateClientAsync("cli1", "aud1", new[] { "https://app/a", "https://app/b" });

        Assert.True(id > 0);
        var list = await admin.GetClientsAsync();
        Assert.Equal(1, list.Count);
        var dto = list.Single();
        Assert.Equal("cli1", dto.ClientId);
        Assert.Contains("https://app/a", dto.AllowedReturnUrls);
    }

    [Fact]
    public async Task GetClientsAsync_Deserializes_Allowed_Return_Urls()
    {
        var dbName = Guid.NewGuid().ToString();
        var root = new InMemoryDatabaseRoot();
        var services = new ServiceCollection();
        services.AddDbContext<AuthDbContext>(opts => opts.UseInMemoryDatabase(dbName, root));
        services.AddScoped<IClientService>(_ => new Mock<IClientService>().Object);
        services.AddScoped<IAdminClientService, ClientAdminService>();
        var provider = services.BuildServiceProvider();

        using (var seedScope = provider.CreateScope())
        {
            var dbSeed = seedScope.ServiceProvider.GetRequiredService<AuthDbContext>();
            dbSeed.ApplicationClients.Add(new ApplicationClient
            {
                ClientId = "cli2",
                Audience = "aud2",
                AllowedReturnUrlsJson = System.Text.Json.JsonSerializer.Serialize(new[] { "https://x/y", "https://x/z" })
            });
            await dbSeed.SaveChangesAsync();
        }
        var admin = provider.GetRequiredService<IAdminClientService>();
        var list = await admin.GetClientsAsync();
        Assert.NotEmpty(list);
        var dto = list.Single(c => c.ClientId == "cli2");
        Assert.Contains("https://x/y", dto.AllowedReturnUrls);
        Assert.Contains("https://x/z", dto.AllowedReturnUrls);
    }
}
