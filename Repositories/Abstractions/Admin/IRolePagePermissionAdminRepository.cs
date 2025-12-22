using Auth.Web.Data.Entities;

namespace Auth.Web.Repositories.Abstractions.Admin;

public interface IRolePagePermissionAdminRepository
{
    Task<List<RolePagePermission>> GetAllAsync(CancellationToken ct = default);
    Task<List<RolePagePermission>> GetByRoleIdAsync(string roleId, CancellationToken ct = default);
    Task<List<RolePagePermission>> GetByPageIdAsync(int pageId, CancellationToken ct = default);
    Task<RolePagePermission?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<RolePagePermission?> FindAsync(string roleId, int pageId, int actionId, CancellationToken ct = default);
    Task<RolePagePermission> CreateAsync(string roleId, int pageId, int actionId, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    Task<bool> DeleteAsync(string roleId, int pageId, int actionId, CancellationToken ct = default);
}
