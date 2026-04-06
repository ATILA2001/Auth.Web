using Auth.Web.Application.Admin.Dtos;

namespace Auth.Web.Services.Abstractions.Admin;

public interface IAdminAreaPagePermissionService
{
    Task<IReadOnlyCollection<AreaPagePermissionAdminDto>> GetPermissionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<AreaPagePermissionAdminDto>> GetPermissionsByAreaAsync(int areaId, CancellationToken cancellationToken = default);
    Task<int> CreatePermissionAsync(int areaId, int? pageId, int? actionId, CancellationToken cancellationToken = default);
    Task UpdatePermissionAsync(int id, int areaId, int? pageId, int? actionId, CancellationToken cancellationToken = default);
    Task DeletePermissionAsync(int id, CancellationToken cancellationToken = default);
}
