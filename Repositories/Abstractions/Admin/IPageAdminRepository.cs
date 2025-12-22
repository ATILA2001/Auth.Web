using Auth.Web.Data.Entities;

namespace Auth.Web.Repositories.Abstractions.Admin;

public interface IPageAdminRepository
{
    Task<List<Page>> GetPagesAsync(CancellationToken ct = default);
    Task<Page?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Page> CreateAsync(string name, string url, CancellationToken ct = default);
    Task<bool> UpdateAsync(int id, string name, string url, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyDictionary<int, int>> GetPagePermissionCountsAsync(CancellationToken ct = default);
    Task<int> GetPagePermissionCountAsync(int pageId, CancellationToken ct = default);
}
