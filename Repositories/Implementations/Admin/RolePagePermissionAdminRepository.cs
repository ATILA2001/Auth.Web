using Auth.Web.Data;
using Auth.Web.Data.Entities;
using Auth.Web.Repositories.Abstractions.Admin;
using Microsoft.EntityFrameworkCore;

namespace Auth.Web.Repositories.Implementations.Admin;

public sealed class RolePagePermissionAdminRepository : IRolePagePermissionAdminRepository
{
    private readonly IDbContextFactory<AuthDbContext> _dbFactory;

    public RolePagePermissionAdminRepository(IDbContextFactory<AuthDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<RolePagePermission>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.RolePagePermissions
            .AsNoTracking()
            .Include(rpp => rpp.Page)
            .Include(rpp => rpp.ActionPermission)
            .OrderBy(rpp => rpp.RoleId)
            .ThenBy(rpp => rpp.Page != null ? rpp.Page.Name : string.Empty)
            .ThenBy(rpp => rpp.ActionPermission != null ? rpp.ActionPermission.Name : string.Empty)
            .ToListAsync(ct);
    }

    public async Task<List<RolePagePermission>> GetByRoleIdAsync(string roleId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.RolePagePermissions
            .AsNoTracking()
            .Include(rpp => rpp.Page)
            .Include(rpp => rpp.ActionPermission)
            .Where(rpp => rpp.RoleId == roleId)
            .OrderBy(rpp => rpp.Page != null ? rpp.Page.Name : string.Empty)
            .ThenBy(rpp => rpp.ActionPermission != null ? rpp.ActionPermission.Name : string.Empty)
            .ToListAsync(ct);
    }

    public async Task<List<RolePagePermission>> GetByPageIdAsync(int pageId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.RolePagePermissions
            .AsNoTracking()
            .Include(rpp => rpp.Page)
            .Include(rpp => rpp.ActionPermission)
            .Where(rpp => rpp.PageId == pageId)
            .OrderBy(rpp => rpp.RoleId)
            .ThenBy(rpp => rpp.ActionPermission != null ? rpp.ActionPermission.Name : string.Empty)
            .ToListAsync(ct);
    }

    public async Task<RolePagePermission?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.RolePagePermissions
            .AsNoTracking()
            .Include(rpp => rpp.Page)
            .Include(rpp => rpp.ActionPermission)
            .FirstOrDefaultAsync(rpp => rpp.Id == id, ct);
    }

    public async Task<RolePagePermission?> FindAsync(string roleId, int pageId, int actionId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.RolePagePermissions
            .AsNoTracking()
            .FirstOrDefaultAsync(rpp => rpp.RoleId == roleId && rpp.PageId == pageId && rpp.ActionPermissionId == actionId, ct);
    }

    public async Task<RolePagePermission> CreateAsync(string roleId, int pageId, int actionId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var permission = new RolePagePermission
        {
            RoleId = roleId,
            PageId = pageId,
            ActionPermissionId = actionId
        };
        db.RolePagePermissions.Add(permission);
        await db.SaveChangesAsync(ct);
        return permission;
    }

    public async Task UpdateAsync(int permissionId, string roleId, int pageId, int actionId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var permission = await db.RolePagePermissions.FindAsync(new object[] { permissionId }, ct);
        if (permission is null)
        {
            throw new InvalidOperationException($"Permission with id {permissionId} not found.");
        }

        permission.RoleId = roleId;
        permission.PageId = pageId;
        permission.ActionPermissionId = actionId;
        
        db.RolePagePermissions.Update(permission);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var permission = await db.RolePagePermissions.FindAsync(new object[] { id }, ct);
        if (permission is null) return false;
        db.RolePagePermissions.Remove(permission);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(string roleId, int pageId, int actionId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var permission = await db.RolePagePermissions
            .FirstOrDefaultAsync(rpp => rpp.RoleId == roleId && rpp.PageId == pageId && rpp.ActionPermissionId == actionId, ct);
        if (permission is null) return false;
        db.RolePagePermissions.Remove(permission);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
