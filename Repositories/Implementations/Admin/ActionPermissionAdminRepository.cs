using Auth.Web.Data;
using Auth.Web.Data.Entities;
using Auth.Web.Repositories.Abstractions.Admin;
using Microsoft.EntityFrameworkCore;

namespace Auth.Web.Repositories.Implementations.Admin;

public sealed class ActionPermissionAdminRepository : IActionPermissionAdminRepository
{
    private readonly IDbContextFactory<AuthDbContext> _dbFactory;

    public ActionPermissionAdminRepository(IDbContextFactory<AuthDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<ActionPermission>> GetActionsAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.ActionPermissions.AsNoTracking().OrderBy(a => a.Name).ToListAsync(ct);
    }

    public async Task<ActionPermission?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.ActionPermissions.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task<ActionPermission> CreateAsync(string name, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var action = new ActionPermission { Name = name.Trim() };
        db.ActionPermissions.Add(action);
        await db.SaveChangesAsync(ct);
        return action;
    }

    public async Task<bool> UpdateAsync(int id, string name, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var action = await db.ActionPermissions.FindAsync(new object[] { id }, ct);
        if (action is null) return false;
        action.Name = name.Trim();
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var action = await db.ActionPermissions.FindAsync(new object[] { id }, ct);
        if (action is null) return false;
        db.ActionPermissions.Remove(action);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyDictionary<int, int>> GetActionUsageCountsAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var counts = await db.RolePagePermissions.AsNoTracking()
            .GroupBy(rpp => rpp.ActionPermissionId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(ct);
        return counts
            .Where(x => x.Key.HasValue)
            .ToDictionary(x => x.Key!.Value, x => x.Count);
    }

    public async Task<int> GetActionUsageCountAsync(int actionId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.RolePagePermissions.CountAsync(rpp => rpp.ActionPermissionId == actionId, ct);
    }
}
