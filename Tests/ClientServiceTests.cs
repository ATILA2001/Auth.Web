using System;
using System.Text.Json;
using System.Threading.Tasks;
using Auth.Web.Data;
using Auth.Web.Data.Entities;
using Auth.Web.Repositories.Implementations.Clients;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Auth.Web.Tests;

public class ClientServiceTests
{
    private static AuthDbContext CreateDb(string dbName)
    {
        var opts = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AuthDbContext(opts);
    }

    [Fact]
    public async Task GetAsync_Returns_Client_WhenExists()
    {
        var dbName = Guid.NewGuid().ToString();
        using var db = CreateDb(dbName);
        var client = new ApplicationClient { ClientId = "cli1", Audience = "aud1", AllowedReturnUrlsJson = JsonSerializer.Serialize(new[] { "https://x/a" }) };
        db.ApplicationClients.Add(client);
        await db.SaveChangesAsync();

        var svc = new Auth.Web.Services.Implementations.Clients.ClientService(new ClientRepository(db));
        var fetched = await svc.GetAsync("cli1");

        Assert.NotNull(fetched);
        Assert.Equal("aud1", fetched!.Audience);
    }

    [Fact]
    public void IsReturnUrlAllowed_Returns_True_For_Matching_Url()
    {
        var db = CreateDb(Guid.NewGuid().ToString());
        var svc = new Auth.Web.Services.Implementations.Clients.ClientService(new ClientRepository(db));
        var client = new ApplicationClient { ClientId = "c1", Audience = "a", AllowedReturnUrlsJson = JsonSerializer.Serialize(new[] { "https://app/x" }) };

        var ok = svc.IsReturnUrlAllowed(client, "https://app/x");
        Assert.True(ok);
    }

    [Fact]
    public void IsReturnUrlAllowed_Returns_False_For_NonMatching_Url()
    {
        var db = CreateDb(Guid.NewGuid().ToString());
        var svc = new Auth.Web.Services.Implementations.Clients.ClientService(new ClientRepository(db));
        var client = new ApplicationClient { ClientId = "c2", Audience = "a", AllowedReturnUrlsJson = JsonSerializer.Serialize(new[] { "https://app/x" }) };

        var ok = svc.IsReturnUrlAllowed(client, "https://other/x");
        Assert.False(ok);
    }

    [Fact]
    public void IsReturnUrlAllowed_Returns_False_For_MalformedJson()
    {
        var db = CreateDb(Guid.NewGuid().ToString());
        var svc = new Auth.Web.Services.Implementations.Clients.ClientService(new ClientRepository(db));
        var client = new ApplicationClient { ClientId = "c3", Audience = "a", AllowedReturnUrlsJson = "NOT JSON" };

        var ok = svc.IsReturnUrlAllowed(client, "https://any");
        Assert.False(ok);
    }
}
