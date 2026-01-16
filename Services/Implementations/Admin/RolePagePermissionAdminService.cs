using Auth.Web.Data.Entities;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Application.Admin.Dtos;
using Auth.Web.Repositories.Abstractions.Admin;
using Microsoft.AspNetCore.Identity;

namespace Auth.Web.Services.Implementations.Admin;

public sealed class RolePagePermissionAdminService : IAdminRolePagePermissionService
{
    private readonly IRolePagePermissionAdminRepository _repository;
    private readonly RoleManager<IdentityRole> _roleManager;

    public RolePagePermissionAdminService(
        IRolePagePermissionAdminRepository repository,
        RoleManager<IdentityRole> roleManager)
    {
        _repository = repository;
        _roleManager = roleManager;
    }

    public async Task<IReadOnlyCollection<RolePagePermissionAdminDto>> GetPermissionsAsync(CancellationToken cancellationToken = default)
    {
        var permissions = await _repository.GetAllAsync(cancellationToken);
        var roleNames = await GetRoleNamesAsync(permissions.Select(p => p.RoleId).Distinct());
        return permissions.Select(p => MapPermission(p, roleNames)).ToList();
    }

    public async Task<IReadOnlyCollection<RolePagePermissionAdminDto>> GetPermissionsByRoleAsync(string roleId, CancellationToken cancellationToken = default)
    {
        var permissions = await _repository.GetByRoleIdAsync(roleId, cancellationToken);
        var roleNames = await GetRoleNamesAsync(new[] { roleId });
        return permissions.Select(p => MapPermission(p, roleNames)).ToList();
    }

    public async Task<IReadOnlyCollection<RolePagePermissionAdminDto>> GetPermissionsByPageAsync(int pageId, CancellationToken cancellationToken = default)
    {
        var permissions = await _repository.GetByPageIdAsync(pageId, cancellationToken);
        var roleNames = await GetRoleNamesAsync(permissions.Select(p => p.RoleId).Distinct());
        return permissions.Select(p => MapPermission(p, roleNames)).ToList();
    }

    public async Task<int> CreatePermissionAsync(string roleId, int pageId, int actionId, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.FindAsync(roleId, pageId, actionId, cancellationToken);
        if (existing is not null)
        {
            return existing.Id;
        }

        var permission = await _repository.CreateAsync(roleId, pageId, actionId, cancellationToken);
        return permission.Id;
    }

    public async Task UpdatePermissionAsync(int permissionId, string roleId, int pageId, int actionId, CancellationToken cancellationToken = default)
    {
        var permission = await _repository.GetByIdAsync(permissionId, cancellationToken);
        if (permission is null)
        {
            throw new InvalidOperationException($"Permission with id {permissionId} not found.");
        }

        // Check for duplicate with different ID
        var duplicate = await _repository.FindAsync(roleId, pageId, actionId, cancellationToken);
        if (duplicate is not null && duplicate.Id != permissionId)
        {
            throw new InvalidOperationException("Ya existe este permiso.");
        }

        await _repository.UpdateAsync(permissionId, roleId, pageId, actionId, cancellationToken);
    }

    public Task DeletePermissionAsync(int permissionId, CancellationToken cancellationToken = default)
        => _repository.DeleteAsync(permissionId, cancellationToken);

    public Task DeletePermissionAsync(string roleId, int pageId, int actionId, CancellationToken cancellationToken = default)
        => _repository.DeleteAsync(roleId, pageId, actionId, cancellationToken);

    private async Task<Dictionary<string, string>> GetRoleNamesAsync(IEnumerable<string> roleIds)
    {
        var result = new Dictionary<string, string>();
        foreach (var roleId in roleIds)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            result[roleId] = role?.Name ?? roleId;
        }
        return result;
    }

    private static RolePagePermissionAdminDto MapPermission(RolePagePermission p, Dictionary<string, string> roleNames) => new()
    {
        Id = p.Id,
        RoleId = p.RoleId,
        RoleName = roleNames.TryGetValue(p.RoleId, out var name) ? name : p.RoleId,
        PageId = p.PageId,
        PageName = p.Page?.Name ?? string.Empty,
        PageUrl = p.Page?.Url ?? string.Empty,
        ActionPermissionId = p.ActionPermissionId,
        ActionName = p.ActionPermission?.Name ?? string.Empty
    };
}
