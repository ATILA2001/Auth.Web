using Auth.Web.Data;
using Auth.Web.Repositories.Abstractions.Admin;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Auth.Web.Repositories.Implementations.Admin;

public sealed class RoleAdminRepository : IRoleAdminRepository
{
    private readonly AuthDbContext _db;

    public RoleAdminRepository(AuthDbContext db)
    {
        _db = db;
    }

    public Task<List<IdentityRole>> GetRolesAsync(CancellationToken ct = default)
        => _db.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync(ct);

    public async Task<IReadOnlyDictionary<string, int>> GetRoleUserCountsAsync(CancellationToken ct = default)
    {
        var counts = await _db.UserRoles.AsNoTracking()
            .GroupBy(ur => ur.RoleId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(ct);
        return counts.ToDictionary(x => x.Key, x => x.Count);
    }

    public Task<int> GetRoleUserCountAsync(string roleId, CancellationToken ct = default)
        => _db.UserRoles.CountAsync(ur => ur.RoleId == roleId, ct);
}
