using Auth.Web.Data.Entities;

namespace Auth.Web.Repositories.Abstractions.Routing;

public interface IRoutingRepository
{
    Task<IReadOnlyCollection<int>> GetUserAreaIdsAsync(string userId, CancellationToken ct = default);
    Task<AreaRoute?> GetFirstActiveRouteForAreasAsync(IEnumerable<int> areaIds, CancellationToken ct = default);
    Task<IReadOnlyList<AreaRoute>> GetDistinctActiveRoutesForAreasAsync(IEnumerable<int> areaIds, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, int>> GetDefaultAreaIdPerClientAsync(IEnumerable<string> clientIds, CancellationToken ct = default);
    Task<IReadOnlyDictionary<int, string>> GetClientIdByAreaIdAsync(IEnumerable<int> areaIds, CancellationToken ct = default);
}