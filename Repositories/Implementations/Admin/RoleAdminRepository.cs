using Auth.Web.Application.Admin.Dtos;
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

    public async Task<IReadOnlyCollection<RoleAdminDto>> GetRolesWithUserCountAsync(CancellationToken ct = default)
    {
        var roles = await _db.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync(ct);
        var userRoleCounts = await _db.UserRoles.AsNoTracking().GroupBy(ur => ur.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() }).ToListAsync(ct);
        var countMap = userRoleCounts.ToDictionary(x => x.RoleId, x => x.Count);
        return roles.Select(r => new RoleAdminDto
        {
            Id = r.Id,
            Name = r.Name ?? string.Empty,
            UserCount = countMap.TryGetValue(r.Id, out var c) ? c : 0
        }).ToList();
    }

    public async Task<RoleAdminDto?> GetRoleByIdWithUserCountAsync(string roleId, CancellationToken ct = default)
    {
        var role = await _db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == roleId, ct);
        if (role is null) return null;
        var count = await _db.UserRoles.CountAsync(ur => ur.RoleId == roleId, ct);
        return new RoleAdminDto { Id = role.Id, Name = role.Name ?? string.Empty, UserCount = count };
    }
}
