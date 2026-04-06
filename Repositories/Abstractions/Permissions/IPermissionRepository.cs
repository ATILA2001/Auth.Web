using Auth.Web.Data.Entities;

namespace Auth.Web.Repositories.Abstractions.Permissions;

public interface IPermissionRepository
{
    Task<IReadOnlyCollection<int>> GetUserAreaIdsAsync(string userId, CancellationToken ct = default);
    Task<IReadOnlyCollection<AreaPagePermission>> GetAreaPagePermissionsAsync(IList<int> areaIds, int? clientId = null, CancellationToken ct = default);
    Task<IReadOnlyCollection<UserPageOverride>> GetUserPageOverridesAsync(string userId, int? clientId = null, CancellationToken ct = default);
    Task<int> GetUserPermissionVersionAsync(string userId, CancellationToken ct = default);
}