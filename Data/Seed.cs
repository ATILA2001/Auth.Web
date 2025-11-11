using System.Text.Json;
using Auth.Web.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.Web.Data;

public static class Seed
{
    public static async Task RunAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        var context = scopedProvider.GetRequiredService<AuthDbContext>();
        await context.Database.MigrateAsync();

        if (!await context.ActionPermissions.AnyAsync())
        {
            var actions = new[] { "Ver", "Agregar", "Editar", "Eliminar" };
            foreach (var action in actions)
            {
                context.ActionPermissions.Add(new ActionPermission { Name = action });
            }
        }

        var roleManager = scopedProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await EnsureRoleAsync(roleManager, "Admin");
        await EnsureRoleAsync(roleManager, "Usuario");

        if (!await context.ApplicationClients.AnyAsync())
        {
            context.ApplicationClients.Add(new ApplicationClient
            {
                ClientId = "FinApp",
                Audience = "finapp",
                AllowedReturnUrlsJson = JsonSerializer.Serialize(new[]
                {
                    "https://finapp.test/return",
                    "https://finapp.test/alt-return"
                })
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }
}
