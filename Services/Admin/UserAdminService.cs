using Auth.Web.Application.Admin.Abstractions;
using Auth.Web.Application.Admin.Dtos;
using Auth.Web.Data;
using Auth.Web.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Auth.Web.Services.Admin;

public sealed class UserAdminService : IUserAdminService, IAdminUserService // Keep legacy IUserAdminService (implicit) + new interface
{
    private readonly AuthDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserAdminService(AuthDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // Legacy method (used by existing UI) - preserved
    public async Task<(List<UserItem> Users, List<string> AllRoles)> GetAsync(CancellationToken ct = default)
    {
        var users = await _db.Users.AsNoTracking().OrderBy(u => u.UserName).ToListAsync(ct);
        var roles = await _db.Roles.AsNoTracking().Select(r => r.Name!).OrderBy(n => n).ToListAsync(ct);
        var userRoles = await _db.UserRoles.AsNoTracking().ToListAsync(ct);
        var roleMap = (await _db.Roles.AsNoTracking().ToListAsync(ct)).ToDictionary(r => r.Id, r => r.Name!);

        var usersOut = users.Select(u =>
        {
            var rs = userRoles.Where(ur => ur.UserId == u.Id).Select(ur => roleMap[ur.RoleId]).OrderBy(n => n).ToList();
            return new UserItem(u.Id, u.UserName, u.Email, rs);
        }).ToList();

        return (usersOut, roles);
    }

    // Legacy method
    public async Task<bool> UpdateRolesAsync(string userId, IEnumerable<string> roles, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return false;

        var current = await _userManager.GetRolesAsync(user);
        var desired = roles.Distinct().ToArray();

        var toAdd = desired.Except(current).ToArray();
        var toRemove = current.Except(desired).ToArray();

        if (toAdd.Length > 0)
            await _userManager.AddToRolesAsync(user, toAdd);
        if (toRemove.Length > 0)
            await _userManager.RemoveFromRolesAsync(user, toRemove);
        return true;
    }

    // New interface implementation
    public async Task<IReadOnlyCollection<UserAdminDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _db.Users.AsNoTracking().OrderBy(u => u.UserName).ToListAsync(cancellationToken);
        var roles = await _db.Roles.AsNoTracking().ToListAsync(cancellationToken);
        var userRoles = await _db.UserRoles.AsNoTracking().ToListAsync(cancellationToken);
        var userAreas = await _db.UserAreas.AsNoTracking().ToListAsync(cancellationToken);
        var areas = await _db.Areas.AsNoTracking().ToListAsync(cancellationToken);
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

    public async Task<UserAdminDto?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null) return null;

        var roleIds = await _db.UserRoles.AsNoTracking().Where(ur => ur.UserId == user.Id).Select(ur => ur.RoleId).ToListAsync(cancellationToken);
        var roles = await _db.Roles.AsNoTracking().Where(r => roleIds.Contains(r.Id)).Select(r => r.Name!).OrderBy(n => n).ToListAsync(cancellationToken);
        var areaIds = await _db.UserAreas.AsNoTracking().Where(ua => ua.UserId == user.Id).Select(ua => ua.AreaId).Distinct().ToListAsync(cancellationToken);
        var areaNames = await _db.Areas.AsNoTracking().Where(a => areaIds.Contains(a.Id)).Select(a => a.Name).OrderBy(n => n).ToListAsync(cancellationToken);

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

    public async Task UpdateUserRolesAsync(string userId, IEnumerable<string> roles, CancellationToken cancellationToken = default)
    {
        await UpdateRolesAsync(userId, roles, cancellationToken);
    }

    public async Task UpdateUserAreasAsync(string userId, IEnumerable<int> areaIds, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return;

        var current = await _db.UserAreas.Where(ua => ua.UserId == userId).Select(ua => ua.AreaId).ToListAsync(cancellationToken);
        var desired = areaIds.Distinct().ToList();

        var toAdd = desired.Except(current).ToList();
        var toRemove = current.Except(desired).ToList();

        if (toAdd.Count > 0)
        {
            foreach (var aid in toAdd)
            {
                _db.UserAreas.Add(new UserArea { UserId = userId, AreaId = aid });
            }
        }
        if (toRemove.Count > 0)
        {
            var removeEntities = await _db.UserAreas.Where(ua => ua.UserId == userId && toRemove.Contains(ua.AreaId)).ToListAsync(cancellationToken);
            _db.UserAreas.RemoveRange(removeEntities);
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateUserRolesAndAreasAsync(string userId, IEnumerable<string> roles, IEnumerable<int> areaIds, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return;

        var currentRoles = await _userManager.GetRolesAsync(user);
        var desiredRoles = roles.Distinct().ToArray();
        var toAddRoles = desiredRoles.Except(currentRoles).ToArray();
        var toRemoveRoles = currentRoles.Except(desiredRoles).ToArray();
        if (toAddRoles.Length > 0) await _userManager.AddToRolesAsync(user, toAddRoles);
        if (toRemoveRoles.Length > 0) await _userManager.RemoveFromRolesAsync(user, toRemoveRoles);

        var currentAreaIds = await _db.UserAreas.Where(ua => ua.UserId == userId).Select(ua => ua.AreaId).ToListAsync(cancellationToken);
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
            var removeEntities = await _db.UserAreas.Where(ua => ua.UserId == userId && toRemoveAreas.Contains(ua.AreaId)).ToListAsync(cancellationToken);
            _db.UserAreas.RemoveRange(removeEntities);
        }
        await _db.SaveChangesAsync(cancellationToken);
    }
}
