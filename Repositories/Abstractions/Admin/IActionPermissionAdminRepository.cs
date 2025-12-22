using Auth.Web.Data.Entities;

namespace Auth.Web.Repositories.Abstractions.Admin;

public interface IActionPermissionAdminRepository
{
    Task<List<ActionPermission>> GetActionsAsync(CancellationToken ct = default);
    Task<ActionPermission?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ActionPermission> CreateAsync(string name, CancellationToken ct = default);
    Task<bool> UpdateAsync(int id, string name, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyDictionary<int, int>> GetActionUsageCountsAsync(CancellationToken ct = default);
    Task<int> GetActionUsageCountAsync(int actionId, CancellationToken ct = default);
}
