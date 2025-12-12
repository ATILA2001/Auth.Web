using Auth.Web.Data.Entities;

namespace Auth.Web.Repositories.Abstractions.Permissions;

public interface IPermissionRepository
{
    Task<IReadOnlyCollection<string>> GetUserRoleIdsAsync(IEnumerable<string> roleNames, CancellationToken ct = default);
    Task<IReadOnlyCollection<int>> GetUserAreaIdsAsync(string userId, CancellationToken ct = default);
    Task<IReadOnlyCollection<RolePagePermission>> GetRolePagePermissionsAsync(IEnumerable<string> roleIds, CancellationToken ct = default);
}