using Auth.Web.Application.Permissions.Dtos;
using Auth.Web.Data.Entities;
using Auth.Web.Repositories.Abstractions.Permissions;
using Auth.Web.Services.Abstractions.Permissions;
using Microsoft.AspNetCore.Identity;

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

    public async Task<UserPermissionsDto> GetAsync(string userName, IReadOnlyCollection<string>? roleNamesOverride = null, IReadOnlyCollection<int>? areaIdsOverride = null)
    {
        var user = await _userManager.FindByNameAsync(userName);
        if (user is null && !string.IsNullOrWhiteSpace(userName))
        {
            user = await _userManager.FindByEmailAsync(userName);
        }
        var resolvedRoleNames = (roleNamesOverride ?? Array.Empty<string>())
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (resolvedRoleNames.Length == 0 && user is null)
        {
            return new UserPermissionsDto
            {
                Pages = new List<PagePermissionDto>(),
                AreaNames = new List<string>(),
                Version = 1
            };
        }

        if (resolvedRoleNames.Length == 0 && user is not null)
        {
            var roleNames = await _userManager.GetRolesAsync(user);
            resolvedRoleNames = roleNames
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        var roleIds = await _repository.GetUserRoleIdsAsync(resolvedRoleNames);
        var areaIds = areaIdsOverride?.ToList()
            ?? (user is not null ? (await _repository.GetUserAreaIdsAsync(user.Id)).ToList() : new List<int>());
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

        var areaIdsList = areaIds.ToList();
        var areaNames = areaIdsList.Count == 0
            ? new List<string>()
            : (await _repository.GetAreaNamesAsync(areaIdsList)).ToList();

        return new UserPermissionsDto
        {
            Pages = pagePermissions,
            AreaNames = areaNames,
            Version = 1
        };
    }

    private static string NormalizePagePath(string url)
    {
        url = url.Trim();
        if (Uri.TryCreate(url, UriKind.Absolute, out var absolute))
        {
            var pathAndQuery = string.IsNullOrWhiteSpace(absolute.PathAndQuery) ? "/" : absolute.PathAndQuery;
            return pathAndQuery.StartsWith("/") ? pathAndQuery : "/" + pathAndQuery;
        }

        if (url.StartsWith("~/")) url = url.Substring(1);
        if (!url.StartsWith("/")) url = "/" + url;
        return url;
    }
}
