using Auth.Web.Data;
using Auth.Web.Data.Entities;
using Auth.Web.Repositories.Abstractions.Admin;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Auth.Web.Repositories.Implementations.Admin;

public sealed class UserAdminRepository : IUserAdminRepository
{
    private readonly AuthDbContext _db;

    public UserAdminRepository(AuthDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyCollection<ApplicationUser>> GetUsersAsync(CancellationToken ct = default)
        => await _db.Users.AsNoTracking().OrderBy(u => u.UserName).ToListAsync(ct);

    public Task<ApplicationUser?> GetUserByIdAsync(string userId, CancellationToken ct = default)
        => _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);

    public async Task<IReadOnlyCollection<IdentityUserRole<string>>> GetUserRolesAsync(IEnumerable<string> userIds, CancellationToken ct = default)
    {
        var ids = Normalize(userIds);
        var query = _db.UserRoles.AsNoTracking();
        if (ids.Length > 0)
        {
            query = query.Where(ur => ids.Contains(ur.UserId));
        }
        return await query.ToListAsync(ct);
    }

    public async Task<IReadOnlyCollection<IdentityRole>> GetRolesByIdsAsync(IEnumerable<string> roleIds, CancellationToken ct = default)
    {
        var ids = Normalize(roleIds);
        if (ids.Length == 0)
        {
            return Array.Empty<IdentityRole>();
        }
        return await _db.Roles.AsNoTracking().Where(r => ids.Contains(r.Id)).ToListAsync(ct);
    }

    public async Task<IReadOnlyCollection<UserArea>> GetUserAreasAsync(IEnumerable<string> userIds, CancellationToken ct = default)
    {
        var ids = Normalize(userIds);
        var query = _db.UserAreas.AsNoTracking();
        if (ids.Length > 0)
        {
            query = query.Where(ua => ids.Contains(ua.UserId));
        }
        return await query.ToListAsync(ct);
    }

    public async Task<IReadOnlyCollection<Area>> GetAreasByIdsAsync(IEnumerable<int> areaIds, CancellationToken ct = default)
    {
        var ids = areaIds?.Distinct().ToArray() ?? Array.Empty<int>();
        if (ids.Length == 0)
        {
            return Array.Empty<Area>();
        }
        return await _db.Areas.AsNoTracking().Where(a => ids.Contains(a.Id)).ToListAsync(ct);
    }

    public async Task UpdateUserAreasAsync(string userId, IEnumerable<int> areaIds, CancellationToken ct = default)
    {
        var currentAreaIds = await _db.UserAreas.Where(ua => ua.UserId == userId).Select(ua => ua.AreaId).ToListAsync(ct);
        var desiredAreaIds = areaIds.Distinct().ToList();
        var toAddAreas = desiredAreaIds.Except(currentAreaIds).ToList();
        var toRemoveAreas = currentAreaIds.Except(desiredAreaIds).ToList();

        if (toAddAreas.Count > 0)
        {
            foreach (var aid in toAddAreas)
            {
                _db.UserAreas.Add(new UserArea { UserId = userId, AreaId = aid });
            }
        }
        if (toRemoveAreas.Count > 0)
        {
            var removeEntities = await _db.UserAreas.Where(ua => ua.UserId == userId && toRemoveAreas.Contains(ua.AreaId)).ToListAsync(ct);
            _db.UserAreas.RemoveRange(removeEntities);
        }
        await _db.SaveChangesAsync(ct);
    }

    private static string[] Normalize(IEnumerable<string> values)
        => values?.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? Array.Empty<string>();
}
