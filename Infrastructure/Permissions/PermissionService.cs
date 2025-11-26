using Auth.Web.Data;
using Auth.Web.Domain.Dtos;
using Auth.Web.Domain.Entities;
using Auth.Web.Utils;
using Auth.Web.Application.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Auth.Web.Infrastructure.Permissions;

public class PermissionService : IPermissionService
{
    private readonly AuthDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public PermissionService(AuthDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<UserPermissionsDto> GetAsync(string userName)
    {
        var user = await _userManager.FindByNameAsync(userName);

        if (user is null)
        {
            return new UserPermissionsDto
            {
                Pages = new List<PagePermissionDto>(),
                Areas = new List<int>(),
                Version = 1
            };
        }

        var roleNames = await _userManager.GetRolesAsync(user);

        var roleIds = roleNames.Count == 0
            ? new List<string>()
            : await _context.Roles
                .Where(role => roleNames.Contains(role.Name!))
                .Select(role => role.Id)
                .ToListAsync();

        var areaIds = await _context.UserAreas
            .Where(ua => ua.UserId == user.Id)
            .Select(ua => ua.AreaId)
            .Distinct()
            .OrderBy(id => id)
            .ToListAsync();

        var rolePermissions = await _context.RolePagePermissions
            .Where(rpp => roleIds.Contains(rpp.RoleId))
            .Select(rpp => new
            {
                PageUrl = rpp.Page != null ? rpp.Page.Url : null,
                ActionName = rpp.ActionPermission != null ? rpp.ActionPermission.Name : null
            })
            .ToListAsync();

        var pagePermissions = rolePermissions
            .Where(x => !string.IsNullOrWhiteSpace(x.PageUrl) && !string.IsNullOrWhiteSpace(x.ActionName))
            .Select(x => new
            {
                Url = Urls.NormalizePagePath(x.PageUrl!),
                Action = x.ActionName!.Trim()
            })
            .GroupBy(x => x.Url, StringComparer.OrdinalIgnoreCase)
            .Select(group => new PagePermissionDto
            {
                Url = group.Key,
                Actions = group
                    .Select(item => item.Action)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(action => action, StringComparer.OrdinalIgnoreCase)
                    .ToList()
            })
            .Where(dto => dto.Actions.Count > 0)
            .OrderBy(dto => dto.Url, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new UserPermissionsDto
        {
            Pages = pagePermissions,
            Areas = areaIds,
            Version = 1
        };
    }
}