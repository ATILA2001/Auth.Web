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

public class ActionPermissionAdminServiceTests
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
        services.AddScoped<IActionPermissionAdminRepository, ActionPermissionAdminRepository>();
        services.AddScoped<IAdminActionPermissionService, ActionPermissionAdminService>();
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task GetActionsAsync_Returns_Empty_When_No_Actions()
    {
        var provider = CreateServiceProvider(Guid.NewGuid().ToString());
        var service = provider.GetRequiredService<IAdminActionPermissionService>();

        var result = await service.GetActionsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetActionsAsync_Returns_Actions_With_Usage_Counts()
    {
        var provider = CreateServiceProvider(Guid.NewGuid().ToString());

        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            var action = new ActionPermission { Id = 1, Name = "Read" };
            db.ActionPermissions.Add(action);
            
            var page = new Page { Id = 1, Name = "Dashboard", Url = "/dashboard" };
            db.Pages.Add(page);
            
            var role = new Microsoft.AspNetCore.Identity.IdentityRole { Id = "role1", Name = "Admin" };
            db.Roles.Add(role);
            
            db.RolePagePermissions.Add(new RolePagePermission 
            { 
                Id = 1,
                RoleId = role.Id, 
                PageId = page.Id, 
                ActionPermissionId = action.Id 
            });
            
            await db.SaveChangesAsync();
        }

        var service = provider.GetRequiredService<IAdminActionPermissionService>();
        var result = await service.GetActionsAsync();

        Assert.Single(result);
        var dto = result.First();
        Assert.Equal("Read", dto.Name);
        Assert.Equal(1, dto.UsageCount);
    }

    [Fact]
    public async Task GetActionByIdAsync_Returns_Null_When_Not_Found()
    {
        var provider = CreateServiceProvider(Guid.NewGuid().ToString());
        var service = provider.GetRequiredService<IAdminActionPermissionService>();

        var result = await service.GetActionByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetActionByIdAsync_Returns_Action_With_Usage_Count()
    {
        var provider = CreateServiceProvider(Guid.NewGuid().ToString());

        int actionId;
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            var action = new ActionPermission { Name = "Write" };
            db.ActionPermissions.Add(action);
            await db.SaveChangesAsync();
            actionId = action.Id;
        }

        var service = provider.GetRequiredService<IAdminActionPermissionService>();
        var result = await service.GetActionByIdAsync(actionId);

        Assert.NotNull(result);
        Assert.Equal("Write", result.Name);
        Assert.Equal(0, result.UsageCount);
    }

    [Fact]
    public async Task CreateActionAsync_Creates_New_Action()
    {
        var provider = CreateServiceProvider(Guid.NewGuid().ToString());
        var service = provider.GetRequiredService<IAdminActionPermissionService>();

        var id = await service.CreateActionAsync("Delete");

        Assert.True(id > 0);

        var actions = await service.GetActionsAsync();
        Assert.Single(actions);
        Assert.Equal("Delete", actions.First().Name);
    }

    [Fact]
    public async Task CreateActionAsync_Returns_Zero_When_Name_Empty()
    {
        var provider = CreateServiceProvider(Guid.NewGuid().ToString());
        var service = provider.GetRequiredService<IAdminActionPermissionService>();

        var id = await service.CreateActionAsync("");

        Assert.Equal(0, id);
    }

    [Fact]
    public async Task CreateActionAsync_Returns_Zero_When_Name_Whitespace()
    {
        var provider = CreateServiceProvider(Guid.NewGuid().ToString());
        var service = provider.GetRequiredService<IAdminActionPermissionService>();

        var id = await service.CreateActionAsync("   ");

        Assert.Equal(0, id);
    }

    [Fact]
    public async Task UpdateActionAsync_Updates_Action_Name()
    {
        var provider = CreateServiceProvider(Guid.NewGuid().ToString());

        int actionId;
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            var action = new ActionPermission { Name = "OldName" };
            db.ActionPermissions.Add(action);
            await db.SaveChangesAsync();
            actionId = action.Id;
        }

        var service = provider.GetRequiredService<IAdminActionPermissionService>();
        await service.UpdateActionAsync(actionId, "NewName");

        var updated = await service.GetActionByIdAsync(actionId);
        Assert.NotNull(updated);
        Assert.Equal("NewName", updated.Name);
    }

    [Fact]
    public async Task DeleteActionAsync_Removes_Action()
    {
        var provider = CreateServiceProvider(Guid.NewGuid().ToString());

        int actionId;
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            var action = new ActionPermission { Name = "ToDelete" };
            db.ActionPermissions.Add(action);
            await db.SaveChangesAsync();
            actionId = action.Id;
        }

        var service = provider.GetRequiredService<IAdminActionPermissionService>();
        await service.DeleteActionAsync(actionId);

        var result = await service.GetActionByIdAsync(actionId);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetActionsAsync_Returns_Actions_Ordered_By_Name()
    {
        var provider = CreateServiceProvider(Guid.NewGuid().ToString());

        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            db.ActionPermissions.Add(new ActionPermission { Name = "Zebra" });
            db.ActionPermissions.Add(new ActionPermission { Name = "Alpha" });
            db.ActionPermissions.Add(new ActionPermission { Name = "Beta" });
            await db.SaveChangesAsync();
        }

        var service = provider.GetRequiredService<IAdminActionPermissionService>();
        var result = await service.GetActionsAsync();

        var names = result.Select(a => a.Name).ToList();
        Assert.Equal(new[] { "Alpha", "Beta", "Zebra" }, names);
    }
}
