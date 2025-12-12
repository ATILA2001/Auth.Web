using Auth.Web.Domain.Dtos;
using Auth.Web.Data.Entities;
using Auth.Web.Services.Abstractions.Permissions;
using Microsoft.AspNetCore.Identity;
using Auth.Web.Repositories.Abstractions.Permissions;

namespace Auth.Web.Services.Implementations.Permissions;

public class PermissionService : IPermissionService
{
    private readonly IPermissionRepository _repository;
    private readonly UserManager<ApplicationUser> _userManager;

    public PermissionService(IPermissionRepository repository, UserManager<ApplicationUser> userManager)
    {
        _repository = repository;
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
        var roleIds = await _repository.GetUserRoleIdsAsync(roleNames);
        var areaIds = await _repository.GetUserAreaIdsAsync(user.Id);
        var rolePermissions = await _repository.GetRolePagePermissionsAsync(roleIds);

        var pagePermissions = rolePermissions
            .Where(rpp => !string.IsNullOrWhiteSpace(rpp.Page?.Url) && !string.IsNullOrWhiteSpace(rpp.ActionPermission?.Name))
            .Select(rpp => new { Url = NormalizePagePath(rpp.Page!.Url), Action = rpp.ActionPermission!.Name!.Trim() })
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
            Areas = areaIds.ToList(),
            Version = 1
        };
    }

    private static string NormalizePagePath(string url)
    {
        url = url.Trim();
        if (url.StartsWith("~/")) url = url.Substring(1);
        if (!url.StartsWith("/")) url = "/" + url;
        return url;
    }
}
