using System.Text.Json;
using Auth.Web.Data.Entities;
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

        // Permisos base
        if (!await context.ActionPermissions.AnyAsync())
        {
            var actions = new[] { "Ver", "Agregar", "Editar", "Eliminar" };
            foreach (var action in actions)
            {
                context.ActionPermissions.Add(new ActionPermission { Name = action });
            }
        }

        // Roles base
        var roleManager = scopedProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await EnsureRoleAsync(roleManager, "Admin");
        await EnsureRoleAsync(roleManager, "Usuario");

        // óreas requeridas
        var areaDgayf = await EnsureAreaAsync(context, "DGAYF");
        var areaContable = await EnsureAreaAsync(context, "CONTABLE");

        // Cliente ónico WebsiteV2
        await EnsureClientAsync(context,
            clientId: "WebsiteV2",
            audience: "websitev2",
            allowedReturnUrls: new[] { "http://10.10.12.37/websitev2", "/websitev2" });

        // Migrar reglas existentes que apunten a "WebsiteV2(borrar)" -> "WebsiteV2"
        var routesToFix = await context.AreaRoutes
            .Where(r => r.ClientId == "WebsiteV2(borrar)")
            .ToListAsync();
        if (routesToFix.Count > 0)
        {
            foreach (var r in routesToFix)
            {
                // Si ya existe una regla equivalente con WebsiteV2, eliminar la obsoleta para evitar duplicados (clave ónica)
                var existsTarget = await context.AreaRoutes.AnyAsync(x => x.AreaId == r.AreaId && x.ClientId == "WebsiteV2" && x.ReturnUrl == r.ReturnUrl);
                if (existsTarget)
                {
                    context.AreaRoutes.Remove(r);
                }
                else
                {
                    r.ClientId = "WebsiteV2";
                }
            }
        }

        // Eliminar cliente obsoleto "WebsiteV2(borrar)" si existe
        var obsoleteClient = await context.ApplicationClients.FirstOrDefaultAsync(c => c.ClientId == "WebsiteV2(borrar)");
        if (obsoleteClient is not null)
        {
            context.ApplicationClients.Remove(obsoleteClient);
        }

        // Reglas de ruteo por órea (si no existen)
        await EnsureAreaRouteAsync(context, areaDgayf.Id, clientId: "WebsiteV2", returnUrl: "http://10.10.12.37/websitev2", priority: 1);
        await EnsureAreaRouteAsync(context, areaContable.Id, clientId: "WebsiteV2", returnUrl: "http://10.10.12.37/websitev2", priority: 1);

        // Usuarios locales y asignación de óreas
        var userManager = scopedProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user1 = await EnsureUserAsync(userManager, "n.carracedo@buenosaires.gob.ar", nombre: "N. Carracedo");
        await EnsureUserAreaAsync(context, user1.Id, areaDgayf.Id);
        await EnsureUserRoleAsync(userManager, user1, "Admin");

        var user2 = await EnsureUserAsync(userManager, "ycaceres@buenosaires.gob.ar", nombre: "Y. Caceres");
        await EnsureUserAreaAsync(context, user2.Id, areaContable.Id);

        // Guardar todo
        await context.SaveChangesAsync();
    }

    private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    private static async Task<Area> EnsureAreaAsync(AuthDbContext context, string name)
    {
        var area = await context.Areas.FirstOrDefaultAsync(a => a.Name == name);
        if (area is null)
        {
            area = new Area { Name = name };
            context.Areas.Add(area);
            await context.SaveChangesAsync();
        }
        return area;
    }

    private static async Task EnsureClientAsync(AuthDbContext context, string clientId, string audience, string[] allowedReturnUrls)
    {
        var client = await context.ApplicationClients.FirstOrDefaultAsync(c => c.ClientId == clientId);
        if (client is null)
        {
            client = new ApplicationClient
            {
                ClientId = clientId,
                Audience = audience,
                AllowedReturnUrlsJson = JsonSerializer.Serialize(allowedReturnUrls)
            };
            context.ApplicationClients.Add(client);
        }
        else
        {
            try
            {
                var existing = JsonSerializer.Deserialize<string[]>(client.AllowedReturnUrlsJson) ?? Array.Empty<string>();
                var merged = existing.Union(allowedReturnUrls, StringComparer.OrdinalIgnoreCase).ToArray();
                client.AllowedReturnUrlsJson = JsonSerializer.Serialize(merged);
            }
            catch
            {
                client.AllowedReturnUrlsJson = JsonSerializer.Serialize(allowedReturnUrls);
            }
        }
    }

    private static async Task EnsureAreaRouteAsync(AuthDbContext context, int areaId, string clientId, string returnUrl, int priority)
    {
        var exists = await context.AreaRoutes.AnyAsync(r => r.AreaId == areaId && r.ClientId == clientId && r.ReturnUrl == returnUrl);
        if (!exists)
        {
            context.AreaRoutes.Add(new AreaRoute
            {
                AreaId = areaId,
                ClientId = clientId,
                ReturnUrl = returnUrl,
                Priority = priority,
                IsActive = true
            });
        }
    }

    private static async Task<ApplicationUser> EnsureUserAsync(UserManager<ApplicationUser> userManager, string email, string? nombre)
    {
        var localPart = email.Split('@')[0];
        var user = await userManager.FindByEmailAsync(email)
                   ?? await userManager.FindByNameAsync(email)
                   ?? await userManager.FindByNameAsync(localPart);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                Nombre = nombre
            };
            var create = await userManager.CreateAsync(user);
            if (!create.Succeeded)
            {
                throw new InvalidOperationException($"No se pudo crear el usuario {email}: {string.Join(", ", create.Errors.Select(e => e.Description))}");
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                user.Email = email;
                await userManager.UpdateAsync(user);
            }
        }

        return user;
    }

    private static async Task EnsureUserAreaAsync(AuthDbContext context, string userId, int areaId)
    {
        var exists = await context.UserAreas.AnyAsync(ua => ua.UserId == userId && ua.AreaId == areaId);
        if (!exists)
        {
            context.UserAreas.Add(new UserArea { UserId = userId, AreaId = areaId });
        }
    }

    private static async Task EnsureUserRoleAsync(UserManager<ApplicationUser> userManager, ApplicationUser user, string role)
    {
        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }
    }
}
