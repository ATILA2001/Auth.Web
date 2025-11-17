using Auth.Web.Data;
using Auth.Web.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Auth.Web.Services.Admin;

public sealed class UserAdminService : IUserAdminService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserAdminService(IServiceScopeFactory scopeFactory, UserManager<ApplicationUser> userManager)
    {
        _scopeFactory = scopeFactory;
        _userManager = userManager;
    }

    public async Task<(List<UserItem> Users, List<string> AllRoles)> GetAsync(CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        var users = await db.Users.AsNoTracking().OrderBy(u => u.UserName).ToListAsync(ct);
        var roles = await db.Roles.AsNoTracking().Select(r => r.Name!).OrderBy(n => n).ToListAsync(ct);
        var userRoles = await db.UserRoles.AsNoTracking().ToListAsync(ct);
        var roleMap = (await db.Roles.AsNoTracking().ToListAsync(ct)).ToDictionary(r => r.Id, r => r.Name!);

        var usersOut = users.Select(u =>
        {
            var rs = userRoles.Where(ur => ur.UserId == u.Id).Select(ur => roleMap[ur.RoleId]).OrderBy(n => n).ToList();
            return new UserItem(u.Id, u.UserName, u.Email, rs);
        }).ToList();

        return (usersOut, roles);
    }

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
}
