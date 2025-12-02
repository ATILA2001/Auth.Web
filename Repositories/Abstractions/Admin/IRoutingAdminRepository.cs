using Auth.Web.Application.Admin.Dtos;

namespace Auth.Web.Repositories.Abstractions.Admin;

public interface IRoutingAdminRepository
{
    Task<IReadOnlyCollection<AreaRouteAdminDto>> GetRoutesAsync(CancellationToken ct = default);
    Task<AreaRouteAdminDto?> GetRouteAsync(int id, CancellationToken ct = default);
    Task<int> CreateRouteAsync(int areaId, int clientId, string returnUrl, int priority, bool isActive, CancellationToken ct = default);
    Task UpdateRouteAsync(int id, int areaId, int clientId, string returnUrl, int priority, bool isActive, CancellationToken ct = default);
    Task DeleteRouteAsync(int id, CancellationToken ct = default);
}
