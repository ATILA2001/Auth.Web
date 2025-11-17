using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Auth.Web.Data;

namespace Auth.Web.Services.Admin;

public interface IRoleAdminService
{
    Task<List<IdentityRole>> GetRolesAsync(CancellationToken ct = default);
    Task<bool> CreateRoleAsync(string name, CancellationToken ct = default);
    Task<bool> DeleteRoleAsync(string roleId, CancellationToken ct = default);
}

public sealed class RoleAdminService : IRoleAdminService
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IServiceScopeFactory _scopeFactory;

    public RoleAdminService(RoleManager<IdentityRole> roleManager, IServiceScopeFactory scopeFactory)
    {
        _roleManager = roleManager;
        _scopeFactory = scopeFactory;
    }

    public async Task<List<IdentityRole>> GetRolesAsync(CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        return await db.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync(ct);
    }

    public async Task<bool> CreateRoleAsync(string name, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        if (await _roleManager.RoleExistsAsync(name)) return true;
        var res = await _roleManager.CreateAsync(new IdentityRole(name));
        return res.Succeeded;
    }

    public async Task<bool> DeleteRoleAsync(string roleId, CancellationToken ct = default)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role is null) return false;
        var res = await _roleManager.DeleteAsync(role);
        return res.Succeeded;
    }
}
