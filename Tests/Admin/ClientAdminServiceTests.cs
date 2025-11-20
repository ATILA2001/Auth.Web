using Auth.Web.Infrastructure.Admin;
using Auth.Web.Data;
using Auth.Web.Domain.Entities;
using Auth.Web.Application.Admin.Abstractions;
using Auth.Web.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Auth.Web.Tests.Admin;

public class ClientAdminServiceTests
{
    private static AuthDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AuthDbContext(opts);
    }

    [Fact]
    public async Task CreateClientAsync_Persists_Client_With_Urls()
    {
        using var db = CreateDb();
        var clientSvc = new Mock<IClientService>();
        IAdminClientService admin = new ClientAdminService(db, clientSvc.Object);
        var id = await admin.CreateClientAsync("cli1", "aud1", new[] { "https://app/a", "https://app/b" });
        Assert.True(id > 0);
        var entity = db.ApplicationClients.Single();
        Assert.Equal("cli1", entity.ClientId);
        Assert.Contains("app/a", entity.AllowedReturnUrlsJson);
    }

    [Fact]
    public async Task GetClientsAsync_Deserializes_Allowed_Return_Urls()
    {
        using var db = CreateDb();
        db.ApplicationClients.Add(new ApplicationClient
        {
            ClientId = "cli2",
            Audience = "aud2",
            AllowedReturnUrlsJson = System.Text.Json.JsonSerializer.Serialize(new[] { "https://x/y", "https://x/z" })
        });
        await db.SaveChangesAsync();
        var clientSvc = new Mock<IClientService>();
        IAdminClientService admin = new ClientAdminService(db, clientSvc.Object);
        var list = await admin.GetClientsAsync();
        var dto = list.Single(c => c.ClientId == "cli2");
        Assert.Contains("https://x/y", dto.AllowedReturnUrls);
        Assert.Contains("https://x/z", dto.AllowedReturnUrls);
    }
}
