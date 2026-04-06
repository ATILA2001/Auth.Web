using Auth.Web.Data;
using Auth.Web.Data.Entities;
using Auth.Web.Repositories.Abstractions.Admin;
using Microsoft.EntityFrameworkCore;

namespace Auth.Web.Repositories.Implementations.Admin;

public sealed class AreaPagePermissionAdminRepository : IAreaPagePermissionAdminRepository
{
    private readonly IDbContextFactory<AuthDbContext> _dbFactory;

    public AreaPagePermissionAdminRepository(IDbContextFactory<AuthDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<AreaPagePermission>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.AreaPagePermissions
            .AsNoTracking()
            .Include(a => a.Area)
            .Include(a => a.Page)
            .Include(a => a.ActionPermission)
            .OrderBy(a => a.Area != null ? a.Area.Name : string.Empty)
            .ThenBy(a => a.Page != null ? a.Page.Name : string.Empty)
            .ThenBy(a => a.ActionPermission != null ? a.ActionPermission.Name : string.Empty)
            .ToListAsync(ct);
    }

    public async Task<List<AreaPagePermission>> GetByAreaIdAsync(int areaId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.AreaPagePermissions
            .AsNoTracking()
            .Include(a => a.Area)
            .Include(a => a.Page)
            .Include(a => a.ActionPermission)
            .Where(a => a.AreaId == areaId)
            .OrderBy(a => a.Page != null ? a.Page.Name : string.Empty)
            .ThenBy(a => a.ActionPermission != null ? a.ActionPermission.Name : string.Empty)
            .ToListAsync(ct);
    }

    public async Task<AreaPagePermission?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.AreaPagePermissions
            .AsNoTracking()
            .Include(a => a.Area)
            .Include(a => a.Page)
            .Include(a => a.ActionPermission)
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task<AreaPagePermission?> FindAsync(int areaId, int? pageId, int? actionId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.AreaPagePermissions
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AreaId == areaId && a.PageId == pageId && a.ActionPermissionId == actionId, ct);
    }

    public async Task<AreaPagePermission> CreateAsync(int areaId, int? pageId, int? actionId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var entity = new AreaPagePermission
        {
            AreaId = areaId,
            PageId = pageId,
            ActionPermissionId = actionId
        };
        db.AreaPagePermissions.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task UpdateAsync(int id, int areaId, int? pageId, int? actionId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var entity = await db.AreaPagePermissions.FindAsync(new object[] { id }, ct);
        if (entity is null)
        {
            throw new InvalidOperationException($"AreaPagePermission with id {id} not found.");
        }

        entity.AreaId = areaId;
        entity.PageId = pageId;
        entity.ActionPermissionId = actionId;
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var entity = await db.AreaPagePermissions.FindAsync(new object[] { id }, ct);
        if (entity is null) return false;
        db.AreaPagePermissions.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
