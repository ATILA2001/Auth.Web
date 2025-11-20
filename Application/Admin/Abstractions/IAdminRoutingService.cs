using Auth.Web.Application.Admin.Dtos;

namespace Auth.Web.Application.Admin.Abstractions;

public interface IAdminRoutingService
{
    Task<IReadOnlyCollection<AreaRouteAdminDto>> GetRoutesAsync(CancellationToken cancellationToken = default);
    Task<AreaRouteAdminDto?> GetRouteAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateRouteAsync(int areaId, int clientId, string returnUrl, int priority, bool isActive, CancellationToken cancellationToken = default);
    Task UpdateRouteAsync(int id, int areaId, int clientId, string returnUrl, int priority, bool isActive, CancellationToken cancellationToken = default);
    Task DeleteRouteAsync(int id, CancellationToken cancellationToken = default);
}
