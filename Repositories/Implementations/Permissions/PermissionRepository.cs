using Auth.Web.Data;
using Auth.Web.Data.Entities;
using Auth.Web.Repositories.Abstractions.Permissions;
using Microsoft.EntityFrameworkCore;

namespace Auth.Web.Repositories.Implementations.Permissions;

public sealed class PermissionRepository : IPermissionRepository
{
    private readonly AuthDbContext _db;

    public PermissionRepository(AuthDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyCollection<int>> GetUserAreaIdsAsync(string userId, CancellationToken ct = default)
    {
        return await _db.UserAreas
            .Where(ua => ua.UserId == userId)
            .Select(ua => ua.AreaId)
            .Distinct()
            .OrderBy(id => id)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyCollection<AreaPagePermission>> GetAreaPagePermissionsAsync(
        IList<int> areaIds, int? clientId = null, CancellationToken ct = default)
    {
        if (areaIds.Count == 0) return Array.Empty<AreaPagePermission>();

        var query = _db.AreaPagePermissions
            .Include(ap => ap.Page)
            .Include(ap => ap.ActionPermission)
            .Where(ap => areaIds.Contains(ap.AreaId));

        if (clientId.HasValue)
            query = query.Where(ap => ap.Page != null && ap.Page.ClientId == clientId.Value);

        return await query.ToListAsync(ct);
    }

    public async Task<IReadOnlyCollection<UserPageOverride>> GetUserPageOverridesAsync(
        string userId, int? clientId = null, CancellationToken ct = default)
    {
        var query = _db.UserPageOverrides
            .Include(o => o.Page)
            .Include(o => o.ActionPermission)
            .Where(o => o.UserId == userId);

        if (clientId.HasValue)
            query = query.Where(o => o.Page != null && o.Page.ClientId == clientId.Value);

        return await query.ToListAsync(ct);
    }

    public async Task<int> GetUserPermissionVersionAsync(string userId, CancellationToken ct = default)
    {
        return await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.PermissionVersion)
            .FirstOrDefaultAsync(ct);
    }
}
