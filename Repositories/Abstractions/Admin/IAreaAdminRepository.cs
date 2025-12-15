using Auth.Web.Data.Entities;

namespace Auth.Web.Repositories.Abstractions.Admin;

public interface IAreaAdminRepository
{
    Task<List<Area>> GetAreasAsync(CancellationToken ct = default);
    Task<Area?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Area> CreateAsync(string name, CancellationToken ct = default);
    Task<bool> UpdateNameAsync(int id, string name, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyDictionary<int, int>> GetAreaUserCountsAsync(CancellationToken ct = default);
    Task<int> GetAreaUserCountAsync(int areaId, CancellationToken ct = default);
}
