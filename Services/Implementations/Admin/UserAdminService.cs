using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Application.Admin.Dtos;
using Auth.Web.Repositories.Abstractions.Admin;
using Auth.Web.Repositories.Abstractions.Routing;
using Auth.Web.Data.Entities;
using Auth.Web.Services.Abstractions.Permissions;
using Microsoft.AspNetCore.Identity;

namespace Auth.Web.Services.Implementations.Admin;

public sealed class UserAdminService : IAdminUserService
{
    private readonly IUserAdminRepository _repository;
    private readonly IRoutingRepository _routingRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPermissionAuditService _auditService;

    public UserAdminService(
        IUserAdminRepository repository,
        IRoutingRepository routingRepository,
        UserManager<ApplicationUser> userManager,
        IPermissionAuditService auditService)
    {
        _repository = repository;
        _routingRepository = routingRepository;
        _userManager = userManager;
        _auditService = auditService;
    }

    public async Task<IReadOnlyCollection<UserAdminDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _repository.GetUsersAsync(cancellationToken);
        if (users.Count == 0)
        {
            return Array.Empty<UserAdminDto>();
        }

        var userIds = users.Select(u => u.Id).ToArray();
        var roleLinks = await _repository.GetUserRolesAsync(userIds, cancellationToken);
        var roleIds = roleLinks.Select(ur => ur.RoleId).Distinct().ToArray();
        var roles = await _repository.GetRolesByIdsAsync(roleIds, cancellationToken);
        var roleMap = roles.ToDictionary(r => r.Id, r => r.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        var rolesByUser = roleLinks
            .GroupBy(ur => ur.UserId)
            .ToDictionary(g => g.Key,
                g => g.Select(ur => roleMap.TryGetValue(ur.RoleId, out var name) ? name : string.Empty)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToList());

        var userAreas = await _repository.GetUserAreasAsync(userIds, cancellationToken);
        var areaIds = userAreas.Select(ua => ua.AreaId).Distinct().ToArray();
        var areas = await _repository.GetAreasByIdsAsync(areaIds, cancellationToken);
        var areaMap = areas.ToDictionary(a => a.Id, a => a.Name);
        var areaIdsByUser = userAreas
            .GroupBy(ua => ua.UserId)
            .ToDictionary(g => g.Key, g => g.Select(ua => ua.AreaId).Distinct().ToList());

        var allAreaIds = areaIdsByUser.Values.SelectMany(ids => ids).Distinct().ToArray();
        var clientIdByAreaId = await _routingRepository.GetClientIdByAreaIdAsync(allAreaIds, cancellationToken);

        return users.Select(user =>
        {
            var userAreaIds = areaIdsByUser.TryGetValue(user.Id, out var ids) ? ids : new List<int>();
            var clientIds = userAreaIds
                .Select(id => clientIdByAreaId.TryGetValue(id, out var cid) ? cid : null)
                .Where(cid => cid is not null)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(cid => cid, StringComparer.OrdinalIgnoreCase)
                .Select(cid => cid!)
                .ToList();

            return MapUser(
                user,
                rolesByUser.TryGetValue(user.Id, out var roleList) ? roleList : new List<string>(),
                userAreaIds,
                areaMap,
                clientIds);
        }).ToList();
    }

    public async Task<UserAdminDto?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetUserByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var rolesLinks = await _repository.GetUserRolesAsync(new[] { user.Id }, cancellationToken);
        var roleIds = rolesLinks.Select(ur => ur.RoleId).Distinct().ToArray();
        var roles = await _repository.GetRolesByIdsAsync(roleIds, cancellationToken);
        var roleMap = roles.ToDictionary(r => r.Id, r => r.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        var roleList = rolesLinks
            .Select(ur => roleMap.TryGetValue(ur.RoleId, out var name) ? name : string.Empty)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var userAreas = await _repository.GetUserAreasAsync(new[] { user.Id }, cancellationToken);
        var areaIds = userAreas.Select(ua => ua.AreaId).Distinct().ToList();
        var areas = await _repository.GetAreasByIdsAsync(areaIds, cancellationToken);
        var areaMap = areas.ToDictionary(a => a.Id, a => a.Name);

        return MapUser(user, roleList, areaIds, areaMap, new List<string>());
    }

    public async Task UpdateUserRolesAndAreasAsync(string userId, IEnumerable<string> roles, IEnumerable<int> areaIds, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return;

        if (isActive.HasValue)
        {
            user.IsActive = isActive.Value;
            var activeResult = await _userManager.UpdateAsync(user);
            if (!activeResult.Succeeded)
            {
                var errors = string.Join("; ", activeResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(errors)
                    ? "No se pudo actualizar el estado del usuario."
                    : errors);
            }
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var desiredRoles = roles.Distinct().ToArray();
        var toAddRoles = desiredRoles.Except(currentRoles).ToArray();
        var toRemoveRoles = currentRoles.Except(desiredRoles).ToArray();
        if (toAddRoles.Length > 0) await _userManager.AddToRolesAsync(user, toAddRoles);
        if (toRemoveRoles.Length > 0) await _userManager.RemoveFromRolesAsync(user, toRemoveRoles);

        await _repository.UpdateUserAreasAsync(userId, areaIds, cancellationToken);
        await _auditService.IncrementUserPermissionVersionAsync(userId, cancellationToken);
    }

    public async Task UpdateUserRolesAndClientAppsAsync(string userId, IEnumerable<string> roles, IEnumerable<string> clientIds, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return;

        var currentRoles = await _userManager.GetRolesAsync(user);
        var desiredRoles = roles.Distinct().ToArray();
        var toAddRoles = desiredRoles.Except(currentRoles).ToArray();
        var toRemoveRoles = currentRoles.Except(desiredRoles).ToArray();
        if (toAddRoles.Length > 0) await _userManager.AddToRolesAsync(user, toAddRoles);
        if (toRemoveRoles.Length > 0) await _userManager.RemoveFromRolesAsync(user, toRemoveRoles);

        var clientList = clientIds.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var defaultAreaIds = await _routingRepository.GetDefaultAreaIdPerClientAsync(clientList, cancellationToken);
        var resolvedAreaIds = clientList
            .Select(cid => defaultAreaIds.TryGetValue(cid, out var aid) ? (int?)aid : null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();

        await _repository.UpdateUserAreasAsync(userId, resolvedAreaIds, cancellationToken);
        await _auditService.IncrementUserPermissionVersionAsync(userId, cancellationToken);
    }

    public async Task DeleteUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return;
        await _userManager.DeleteAsync(user);
    }

    private static UserAdminDto MapUser(ApplicationUser user, List<string> roles, List<int> areaIds, IReadOnlyDictionary<int, string> areaNameMap, List<string> clientIds)
    {
        var areaNames = areaIds
            .Select(id => areaNameMap.TryGetValue(id, out var name) ? name : string.Empty)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new UserAdminDto
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            FullName = user.Nombre,
            Email = user.Email,
            Roles = roles,
            Areas = areaNames,
            AreaIds = areaIds,
            ClientIds = clientIds,
            PermissionVersion = user.PermissionVersion,
            IsActive = user.IsActive
        };
    }
}
