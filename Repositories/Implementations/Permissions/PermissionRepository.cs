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

    public async Task<IReadOnlyCollection<string>> GetUserRoleIdsAsync(IEnumerable<string> roleNames, CancellationToken ct = default)
    {
        var names = roleNames?.ToArray() ?? Array.Empty<string>();
        if (names.Length == 0) return Array.Empty<string>();
        return await _db.Roles
            .Where(role => names.Contains(role.Name!))
            .Select(role => role.Id)
            .ToListAsync(ct);
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

    public async Task<IReadOnlyCollection<string>> GetAreaNamesAsync(IEnumerable<int> areaIds, CancellationToken ct = default)
    {
        var ids = areaIds?.ToArray() ?? Array.Empty<int>();
        if (ids.Length == 0) return Array.Empty<string>();

        return await _db.Areas
            .Where(a => ids.Contains(a.Id))
            .OrderBy(a => a.Id)
            .Select(a => a.Name)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyCollection<RolePagePermission>> GetRolePagePermissionsAsync(IEnumerable<string> roleIds, CancellationToken ct = default)
    {
        var ids = roleIds?.ToArray() ?? Array.Empty<string>();
        if (ids.Length == 0) return Array.Empty<RolePagePermission>();

        return await _db.RolePagePermissions
            .Where(rpp => ids.Contains(rpp.RoleId))
            .Include(rpp => rpp.Page)
            .Include(rpp => rpp.ActionPermission)
            .ToListAsync(ct);
    }
}
