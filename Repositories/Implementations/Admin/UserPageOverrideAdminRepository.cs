using Auth.Web.Data;
using Auth.Web.Data.Entities;
using Auth.Web.Repositories.Abstractions.Admin;
using Microsoft.EntityFrameworkCore;

namespace Auth.Web.Repositories.Implementations.Admin;

public sealed class UserPageOverrideAdminRepository : IUserPageOverrideAdminRepository
{
    private readonly IDbContextFactory<AuthDbContext> _dbFactory;

    public UserPageOverrideAdminRepository(IDbContextFactory<AuthDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IReadOnlyCollection<UserPageOverride>> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.UserPageOverrides
            .AsNoTracking()
            .Include(o => o.Page)
            .Include(o => o.ActionPermission)
            .Where(o => o.UserId == userId)
            .OrderBy(o => o.Page != null ? o.Page.Name : string.Empty)
            .ThenBy(o => o.ActionPermission != null ? o.ActionPermission.Name : string.Empty)
            .ToListAsync(ct);
    }

    public async Task<UserPageOverride?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.UserPageOverrides
            .AsNoTracking()
            .Include(o => o.Page)
            .Include(o => o.ActionPermission)
            .FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public async Task<UserPageOverride?> FindAsync(string userId, int? pageId, int? actionPermissionId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.UserPageOverrides
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.UserId == userId && o.PageId == pageId && o.ActionPermissionId == actionPermissionId, ct);
    }

    public async Task<int> CreateAsync(string userId, int? pageId, int? actionPermissionId, bool isAllowed, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var entity = new UserPageOverride
        {
            UserId = userId,
            PageId = pageId,
            ActionPermissionId = actionPermissionId,
            IsAllowed = isAllowed
        };
        db.UserPageOverrides.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var entity = await db.UserPageOverrides.FindAsync(new object[] { id }, ct);
        if (entity is null) return;
        db.UserPageOverrides.Remove(entity);
        await db.SaveChangesAsync(ct);
    }
}
