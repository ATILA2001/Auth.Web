using Auth.Web.Data.Entities;

namespace Auth.Web.Repositories.Abstractions.Routing;

public interface IRoutingRepository
{
    Task<IReadOnlyCollection<int>> GetUserAreaIdsAsync(string userId, CancellationToken ct = default);
    Task<AreaRoute?> GetFirstActiveRouteForAreasAsync(IEnumerable<int> areaIds, CancellationToken ct = default);
}