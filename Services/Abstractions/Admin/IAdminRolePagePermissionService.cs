using Auth.Web.Application.Admin.Dtos;

namespace Auth.Web.Services.Abstractions.Admin;

public interface IAdminRolePagePermissionService
{
    Task<IReadOnlyCollection<RolePagePermissionAdminDto>> GetPermissionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<RolePagePermissionAdminDto>> GetPermissionsByRoleAsync(string roleId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<RolePagePermissionAdminDto>> GetPermissionsByPageAsync(int pageId, CancellationToken cancellationToken = default);
    Task<int> CreatePermissionAsync(string roleId, int pageId, int actionId, CancellationToken cancellationToken = default);
    Task DeletePermissionAsync(int permissionId, CancellationToken cancellationToken = default);
    Task DeletePermissionAsync(string roleId, int pageId, int actionId, CancellationToken cancellationToken = default);
}
