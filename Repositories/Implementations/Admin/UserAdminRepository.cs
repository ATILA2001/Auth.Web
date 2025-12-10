using Auth.Web.Application.Admin.Dtos;
using Auth.Web.Data;
using Auth.Web.Data.Entities;
using Auth.Web.Repositories.Abstractions.Admin;
using Microsoft.EntityFrameworkCore;

namespace Auth.Web.Repositories.Implementations.Admin;

public sealed class UserAdminRepository : IUserAdminRepository
{
    private readonly AuthDbContext _db;

    public UserAdminRepository(AuthDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyCollection<UserAdminDto>> GetUsersAsync(CancellationToken ct = default)
    {
        var users = await _db.Users.AsNoTracking().OrderBy(u => u.UserName).ToListAsync(ct);
        var roles = await _db.Roles.AsNoTracking().ToListAsync(ct);
        var userRoles = await _db.UserRoles.AsNoTracking().ToListAsync(ct);
        var userAreas = await _db.UserAreas.AsNoTracking().ToListAsync(ct);
        var areas = await _db.Areas.AsNoTracking().ToListAsync(ct);
        var roleMap = roles.ToDictionary(r => r.Id, r => r.Name!);
        var areaMap = areas.ToDictionary(a => a.Id, a => a.Name);

        var dtos = users.Select(u =>
        {
            var rs = userRoles.Where(ur => ur.UserId == u.Id).Select(ur => roleMap[ur.RoleId]).OrderBy(n => n).ToList();
            var areaIds = userAreas.Where(ua => ua.UserId == u.Id).Select(ua => ua.AreaId).Distinct().ToList();
            var areaNames = areaIds.Select(id => areaMap[id]).OrderBy(n => n).ToList();
            return new UserAdminDto
            {
                Id = u.Id,
                UserName = u.UserName ?? string.Empty,
                Email = u.Email,
                Roles = rs,
                Areas = areaNames,
                AreaIds = areaIds
            };
        }).ToList();

        return dtos;
    }

    public async Task<UserAdminDto?> GetUserByIdAsync(string userId, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return null;

        var roleIds = await _db.UserRoles.AsNoTracking().Where(ur => ur.UserId == user.Id).Select(ur => ur.RoleId).ToListAsync(ct);
        var roles = await _db.Roles.AsNoTracking().Where(r => roleIds.Contains(r.Id)).Select(r => r.Name!).OrderBy(n => n).ToListAsync(ct);
        var areaIds = await _db.UserAreas.AsNoTracking().Where(ua => ua.UserId == user.Id).Select(ua => ua.AreaId).Distinct().ToListAsync(ct);
        var areaNames = await _db.Areas.AsNoTracking().Where(a => areaIds.Contains(a.Id)).Select(a => a.Name).OrderBy(n => n).ToListAsync(ct);

        return new UserAdminDto
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email,
            Roles = roles,
            Areas = areaNames,
            AreaIds = areaIds
        };
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
}
