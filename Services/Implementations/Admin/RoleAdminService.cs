using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Auth.Web.Data;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Application.Admin.Dtos;

namespace Auth.Web.Services.Implementations.Admin;

public sealed class RoleAdminService : IAdminRoleService
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

    async Task<IReadOnlyCollection<RoleAdminDto>> IAdminRoleService.GetRolesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        
        var roles = await db.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync(cancellationToken);
        var userRoleCounts = await db.UserRoles.AsNoTracking().GroupBy(ur => ur.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() }).ToListAsync(cancellationToken);
        var countMap = userRoleCounts.ToDictionary(x => x.RoleId, x => x.Count);
        return roles.Select(r => new RoleAdminDto
        {
            Id = r.Id,
            Name = r.Name ?? string.Empty,
            UserCount = countMap.TryGetValue(r.Id, out var c) ? c : 0
        }).ToList();
    }

    async Task<RoleAdminDto?> IAdminRoleService.GetRoleByIdAsync(string roleId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        
        var role = await db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);
        if (role is null) return null;
        var count = await db.UserRoles.CountAsync(ur => ur.RoleId == roleId, cancellationToken);
        return new RoleAdminDto { Id = role.Id, Name = role.Name ?? string.Empty, UserCount = count };
    }

    async Task<string> IAdminRoleService.CreateRoleAsync(string name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
        if (await _roleManager.RoleExistsAsync(name))
        {
            var existing = await _roleManager.FindByNameAsync(name);
            return existing?.Id ?? string.Empty;
        }
        var res = await _roleManager.CreateAsync(new IdentityRole(name.Trim()));
        return res.Succeeded ? (await _roleManager.FindByNameAsync(name.Trim()))!.Id : string.Empty;
    }

    async Task IAdminRoleService.RenameRoleAsync(string roleId, string newName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(newName)) return;
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role is null) return;
        role.Name = newName.Trim();
        await _roleManager.UpdateAsync(role);
    }

    async Task IAdminRoleService.DeleteRoleAsync(string roleId, CancellationToken cancellationToken)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role is null) return;
        await _roleManager.DeleteAsync(role);
    }
}
