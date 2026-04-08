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

    public async Task<UserPermissionsDto> GetAsync(
        string userName,
        int? clientId = null,
        IReadOnlyCollection<string>? roleNamesOverride = null,
        IReadOnlyCollection<int>? areaIdsOverride = null)
    {
        var user = await _userManager.FindByNameAsync(userName);
        if (user is null && !string.IsNullOrWhiteSpace(userName))
            user = await _userManager.FindByEmailAsync(userName);

        var resolvedRoleNames = (roleNamesOverride ?? Array.Empty<string>())
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (resolvedRoleNames.Length == 0 && user is not null)
        {
            var roleNames = await _userManager.GetRolesAsync(user);
            resolvedRoleNames = roleNames
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        var isAdmin = resolvedRoleNames.Any(r =>
            r.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("Administrador", StringComparison.OrdinalIgnoreCase));

        var permissionVersion = user is not null
            ? await _repository.GetUserPermissionVersionAsync(user.Id)
            : 1;

        // Admin: bypass completo, perms_json vacío
        if (isAdmin || user is null)
        {
            return new UserPermissionsDto
            {
                Pages = [],
                AreaIds = [],
                Version = permissionVersion
            };
        }

        var areaIds = areaIdsOverride?.ToList()
            ?? (await _repository.GetUserAreaIdsAsync(user.Id)).ToList();

        if (areaIds.Count == 0)
        {
            return new UserPermissionsDto
            {
                Pages = [],
                AreaIds = [],
                Version = permissionVersion
            };
        }

        var areaPermissions = await _repository.GetAreaPagePermissionsAsync(areaIds, clientId);
        var userOverrides = await _repository.GetUserPageOverridesAsync(user.Id, clientId);
        var pages = ApplyPermissionRules(areaPermissions, userOverrides);

        return new UserPermissionsDto
        {
            Pages = pages,
            AreaIds = areaIds,
            Version = permissionVersion
        };
    }

    public async Task<int> GetVersionAsync(string userName)
    {
        var user = await _userManager.FindByNameAsync(userName);
        if (user is null && !string.IsNullOrWhiteSpace(userName))
            user = await _userManager.FindByEmailAsync(userName);
        if (user is null) return 1;
        return await _repository.GetUserPermissionVersionAsync(user.Id);
    }

    public Task<int> GetVersionByUserIdAsync(string userId)
        => _repository.GetUserPermissionVersionAsync(userId);

    private static List<PagePermissionDto> ApplyPermissionRules(
        IReadOnlyCollection<AreaPagePermission> areaPermissions,
        IReadOnlyCollection<UserPageOverride> userOverrides)
    {
        // Todas las páginas candidatas: de área + de GRANT overrides
        var pageIds = new HashSet<int>();
        foreach (var ap in areaPermissions.Where(a => a.PageId.HasValue))
            pageIds.Add(ap.PageId!.Value);
        foreach (var ov in userOverrides.Where(o => o.PageId.HasValue && o.Type == "GRANT"))
            pageIds.Add(ov.PageId!.Value);

        var denies = userOverrides
            .Where(o => o.Type == "DENY" && o.PageId.HasValue)
            .ToLookup(o => o.PageId!.Value);

        var grants = userOverrides
            .Where(o => o.Type == "GRANT" && o.PageId.HasValue && o.ActionPermission?.Name != null)
            .ToLookup(o => o.PageId!.Value);

        var result = new List<PagePermissionDto>();

        foreach (var pageId in pageIds)
        {
            // Regla 1: DENY sin ActionId = denegar toda la página (máxima precedencia)
            if (denies[pageId].Any(d => d.ActionPermissionId == null))
                continue;

            // Acciones base desde permisos de área (unión de todas las áreas del usuario)
            var actions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var ap in areaPermissions.Where(a => a.PageId == pageId && a.ActionPermission?.Name != null))
                actions.Add(ap.ActionPermission!.Name!.Trim());

            // Regla 1b: DENY con ActionId específico = remover esa acción
            foreach (var deny in denies[pageId].Where(d => d.ActionPermission?.Name != null))
                actions.Remove(deny.ActionPermission!.Name!.Trim());

            // Regla 2: GRANT agrega acción si no está denegada específicamente
            foreach (var grant in grants[pageId])
            {
                var actionName = grant.ActionPermission!.Name!.Trim();
                var isSpecificallyDenied = denies[pageId].Any(d =>
                    d.ActionPermission?.Name != null &&
                    d.ActionPermission.Name.Equals(actionName, StringComparison.OrdinalIgnoreCase));
                if (!isSpecificallyDenied)
                    actions.Add(actionName);
            }

            if (actions.Count == 0) continue;

            var pageUrl = areaPermissions.FirstOrDefault(a => a.PageId == pageId)?.Page?.Url
                       ?? userOverrides.FirstOrDefault(o => o.PageId == pageId)?.Page?.Url;
            if (string.IsNullOrWhiteSpace(pageUrl)) continue;

            result.Add(new PagePermissionDto
            {
                Url = NormalizePagePath(pageUrl),
                Actions = [.. actions.OrderBy(a => a, StringComparer.OrdinalIgnoreCase)]
            });
        }

        return [.. result.OrderBy(p => p.Url, StringComparer.OrdinalIgnoreCase)];
    }

    private static string NormalizePagePath(string url)
    {
        url = url.Trim();
        if (Uri.TryCreate(url, UriKind.Absolute, out var absolute))
        {
            var pathAndQuery = string.IsNullOrWhiteSpace(absolute.PathAndQuery) ? "/" : absolute.PathAndQuery;
            return pathAndQuery.StartsWith('/') ? pathAndQuery : "/" + pathAndQuery;
        }

        if (url.StartsWith("~/")) url = url[1..];
        if (!url.StartsWith('/')) url = "/" + url;
        return url;
    }
}
