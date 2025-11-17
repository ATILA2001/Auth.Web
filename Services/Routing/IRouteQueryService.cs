using Auth.Web.Domain.Entities;

namespace Auth.Web.Services.Abstractions;

public interface IRouteQueryService
{
    Task<IReadOnlyList<UserRouteInfo>> GetUserRoutesAsync(string userId, CancellationToken ct = default);
}

public sealed record UserRouteInfo(int AreaId, string AreaName, string ClientId, string ReturnUrl, int Priority, bool IsActive);
