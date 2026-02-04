using Auth.Web.Data.Entities;

namespace Auth.Web.Repositories.Abstractions.Admin;

public interface IRoutingAdminRepository
{
    Task<IReadOnlyCollection<AreaRoute>> GetRoutesAsync(CancellationToken ct = default);
    Task<AreaRoute?> GetRouteAsync(int id, CancellationToken ct = default);
    Task<int> CreateRouteAsync(int areaId, int clientId, int priority, bool isActive, CancellationToken ct = default);
    Task UpdateRouteAsync(int id, int areaId, int clientId, int priority, bool isActive, CancellationToken ct = default);
    Task DeleteRouteAsync(int id, CancellationToken ct = default);
}
