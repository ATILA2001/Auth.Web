using Auth.Web.Services.Implementations.Admin;
using Auth.Web.Data;
using Auth.Web.Data.Entities;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Repositories.Abstractions.Admin;
using Auth.Web.Repositories.Implementations.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace Auth.Web.Tests.Admin;

public class PageAdminServiceTests
{
    // Use a shared database root to avoid creating too many service providers
    private static readonly InMemoryDatabaseRoot SharedRoot = new();

    private static IServiceProvider CreateServiceProvider(string dbName)
    {
        var services = new ServiceCollection();
        services.AddDbContext<AuthDbContext>(opts => 
            opts.UseInMemoryDatabase(dbName, SharedRoot)
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning)));
        services.AddDbContextFactory<AuthDbContext>(opts => 
            opts.UseInMemoryDatabase(dbName, SharedRoot)
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning)));
        services.AddScoped<IPageAdminRepository, PageAdminRepository>();
        services.AddScoped<IAdminPageService, PageAdminService>();
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task GetPagesAsync_Returns_Empty_When_No_Pages()
    {
        var provider = CreateServiceProvider("GetPagesAsync_Returns_Empty_When_No_Pages");
        var service = provider.GetRequiredService<IAdminPageService>();

        var result = await service.GetPagesAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPagesAsync_Returns_Pages_With_Permission_Counts()
    {
        var provider = CreateServiceProvider("GetPagesAsync_Returns_Pages_With_Permission_Counts");

        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            var page = new Page { Id = 1, Name = "Dashboard", Url = "/dashboard" };
            db.Pages.Add(page);

            var action = new ActionPermission { Id = 1, Name = "Read" };
            db.ActionPermissions.Add(action);

            var role = new Microsoft.AspNetCore.Identity.IdentityRole { Id = "role1", Name = "User" };
            db.Roles.Add(role);

            db.RolePagePermissions.Add(new RolePagePermission
            {
                Id = 1,
                RoleId = role.Id,
                PageId = page.Id,
                ActionPermissionId = action.Id
            });

            db.RolePagePermissions.Add(new RolePagePermission
            {
                Id = 2,
                RoleId = role.Id,
                PageId = page.Id,
                ActionPermissionId = action.Id
            });

            await db.SaveChangesAsync();
        }

        var service = provider.GetRequiredService<IAdminPageService>();
        var result = await service.GetPagesAsync();

        Assert.Single(result);
        var dto = result.First();
        Assert.Equal("Dashboard", dto.Name);
        Assert.Equal("/dashboard", dto.Url);
        Assert.Equal(2, dto.PermissionCount);
    }

    [Fact]
    public async Task GetPageByIdAsync_Returns_Null_When_Not_Found()
    {
        var provider = CreateServiceProvider("GetPageByIdAsync_Returns_Null_When_Not_Found");
        var service = provider.GetRequiredService<IAdminPageService>();

        var result = await service.GetPageByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPageByIdAsync_Returns_Page_With_Permission_Count()
    {
        var provider = CreateServiceProvider("GetPageByIdAsync_Returns_Page_With_Permission_Count");

        int pageId;
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            var page = new Page { Name = "Settings", Url = "/settings" };
            db.Pages.Add(page);
            await db.SaveChangesAsync();
            pageId = page.Id;
        }

        var service = provider.GetRequiredService<IAdminPageService>();
        var result = await service.GetPageByIdAsync(pageId);

        Assert.NotNull(result);
        Assert.Equal("Settings", result.Name);
        Assert.Equal("/settings", result.Url);
        Assert.Equal(0, result.PermissionCount);
    }

    [Fact]
    public async Task CreatePageAsync_Creates_Page_With_Name_And_Url()
    {
        var provider = CreateServiceProvider("CreatePageAsync_Creates_Page_With_Name_And_Url");
        var service = provider.GetRequiredService<IAdminPageService>();

        var id = await service.CreatePageAsync("Reports", "/reports");

        Assert.True(id > 0);

        var pages = await service.GetPagesAsync();
        Assert.Single(pages);
        var page = pages.First();
        Assert.Equal("Reports", page.Name);
        Assert.Equal("/reports", page.Url);
    }

    [Fact]
    public async Task CreatePageAsync_Returns_Zero_When_Name_Empty()
    {
        var provider = CreateServiceProvider("CreatePageAsync_Returns_Zero_When_Name_Empty");
        var service = provider.GetRequiredService<IAdminPageService>();

        var id = await service.CreatePageAsync("", "/url");

        Assert.Equal(0, id);
    }

    [Fact]
    public async Task CreatePageAsync_Returns_Zero_When_Url_Empty()
    {
        var provider = CreateServiceProvider("CreatePageAsync_Returns_Zero_When_Url_Empty");
        var service = provider.GetRequiredService<IAdminPageService>();

        var id = await service.CreatePageAsync("Name", "");

        Assert.Equal(0, id);
    }

    [Fact]
    public async Task CreatePageAsync_Returns_Zero_When_Both_Empty()
    {
        var provider = CreateServiceProvider("CreatePageAsync_Returns_Zero_When_Both_Empty");
        var service = provider.GetRequiredService<IAdminPageService>();

        var id = await service.CreatePageAsync("", "");

        Assert.Equal(0, id);
    }

    [Fact]
    public async Task UpdatePageAsync_Updates_Page()
    {
        var provider = CreateServiceProvider("UpdatePageAsync_Updates_Page");

        int pageId;
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            var page = new Page { Name = "Old", Url = "/old" };
            db.Pages.Add(page);
            await db.SaveChangesAsync();
            pageId = page.Id;
        }

        var service = provider.GetRequiredService<IAdminPageService>();
        await service.UpdatePageAsync(pageId, "New", "/new");

        var updated = await service.GetPageByIdAsync(pageId);
        Assert.NotNull(updated);
        Assert.Equal("New", updated.Name);
        Assert.Equal("/new", updated.Url);
    }

    [Fact]
    public async Task DeletePageAsync_Removes_Page()
    {
        var provider = CreateServiceProvider("DeletePageAsync_Removes_Page");

        int pageId;
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            var page = new Page { Name = "ToDelete", Url = "/delete" };
            db.Pages.Add(page);
            await db.SaveChangesAsync();
            pageId = page.Id;
        }

        var service = provider.GetRequiredService<IAdminPageService>();
        await service.DeletePageAsync(pageId);

        var result = await service.GetPageByIdAsync(pageId);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPagesAsync_Returns_Pages_Ordered_By_Name()
    {
        var provider = CreateServiceProvider("GetPagesAsync_Returns_Pages_Ordered_By_Name");

        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            db.Pages.Add(new Page { Name = "Zebra", Url = "/z" });
            db.Pages.Add(new Page { Name = "Alpha", Url = "/a" });
            db.Pages.Add(new Page { Name = "Beta", Url = "/b" });
            await db.SaveChangesAsync();
        }

        var service = provider.GetRequiredService<IAdminPageService>();
        var result = await service.GetPagesAsync();

        var names = result.Select(p => p.Name).ToList();
        Assert.Equal(new[] { "Alpha", "Beta", "Zebra" }, names);
    }
}
