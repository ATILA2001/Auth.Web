using Auth.Web.Data.Entities;

namespace Auth.Web.Repositories.Abstractions.Admin;

public interface IUserPageOverrideAdminRepository
{
    Task<IReadOnlyCollection<UserPageOverride>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<UserPageOverride?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<UserPageOverride?> FindAsync(string userId, int? pageId, int? actionPermissionId, CancellationToken ct = default);
    Task<int> CreateAsync(string userId, int? pageId, int? actionPermissionId, bool isAllowed, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
