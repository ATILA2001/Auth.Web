using Auth.Web.Data.Entities;

namespace Auth.Web.Repositories.Abstractions.Admin;

public interface IAreaPagePermissionAdminRepository
{
    Task<List<AreaPagePermission>> GetAllAsync(CancellationToken ct = default);
    Task<List<AreaPagePermission>> GetByAreaIdAsync(int areaId, CancellationToken ct = default);
    Task<AreaPagePermission?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<AreaPagePermission?> FindAsync(int areaId, int? pageId, int? actionId, CancellationToken ct = default);
    Task<AreaPagePermission> CreateAsync(int areaId, int? pageId, int? actionId, CancellationToken ct = default);
    Task UpdateAsync(int id, int areaId, int? pageId, int? actionId, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
