using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Application.Admin.Dtos;
using Auth.Web.Data;
using Auth.Web.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Auth.Web.Services.Implementations.Admin;

public sealed class UserAdminService : IAdminUserService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserAdminService(IServiceScopeFactory scopeFactory, UserManager<ApplicationUser> userManager)
    {
        _scopeFactory = scopeFactory;
        _userManager = userManager;
    }

    public async Task<IReadOnlyCollection<UserAdminDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        
        var users = await db.Users.AsNoTracking().OrderBy(u => u.UserName).ToListAsync(cancellationToken);
        var roles = await db.Roles.AsNoTracking().ToListAsync(cancellationToken);
        var userRoles = await db.UserRoles.AsNoTracking().ToListAsync(cancellationToken);
        var userAreas = await db.UserAreas.AsNoTracking().ToListAsync(cancellationToken);
        var areas = await db.Areas.AsNoTracking().ToListAsync(cancellationToken);
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
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null) return null;

        var roleIds = await db.UserRoles.AsNoTracking().Where(ur => ur.UserId == user.Id).Select(ur => ur.RoleId).ToListAsync(cancellationToken);
        var roles = await db.Roles.AsNoTracking().Where(r => roleIds.Contains(r.Id)).Select(r => r.Name!).OrderBy(n => n).ToListAsync(cancellationToken);
        var areaIds = await db.UserAreas.AsNoTracking().Where(ua => ua.UserId == user.Id).Select(ua => ua.AreaId).Distinct().ToListAsync(cancellationToken);
        var areaNames = await db.Areas.AsNoTracking().Where(a => areaIds.Contains(a.Id)).Select(a => a.Name).OrderBy(n => n).ToListAsync(cancellationToken);

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

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        var currentAreaIds = await db.UserAreas.Where(ua => ua.UserId == userId).Select(ua => ua.AreaId).ToListAsync(cancellationToken);
        var desiredAreaIds = areaIds.Distinct().ToList();
        var toAddAreas = desiredAreaIds.Except(currentAreaIds).ToList();
        var toRemoveAreas = currentAreaIds.Except(desiredAreaIds).ToList();

        if (toAddAreas.Count > 0)
        {
            foreach (var aid in toAddAreas)
            {
                db.UserAreas.Add(new UserArea { UserId = userId, AreaId = aid });
            }
        }
        if (toRemoveAreas.Count > 0)
        {
            var removeEntities = await db.UserAreas.Where(ua => ua.UserId == userId && toRemoveAreas.Contains(ua.AreaId)).ToListAsync(cancellationToken);
            db.UserAreas.RemoveRange(removeEntities);
        }
        await db.SaveChangesAsync(cancellationToken);
    }
}
