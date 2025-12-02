using Auth.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Auth.Web.Services.Implementations.Routing;

public sealed class RouteQueryService
{
    private readonly IServiceScopeFactory _scopeFactory;
    public RouteQueryService(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public async Task<IReadOnlyList<UserRouteInfo>> GetUserRoutesAsync(string userId, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        var areaIds = await db.UserAreas
            .AsNoTracking()
            .Where(ua => ua.UserId == userId)
            .Select(ua => ua.AreaId)
            .ToListAsync(ct);

        if (areaIds.Count == 0) return Array.Empty<UserRouteInfo>();

        var routes = await db.AreaRoutes
            .AsNoTracking()
            .Where(r => areaIds.Contains(r.AreaId))
            .OrderBy(r => r.Priority)
            .ToListAsync(ct);

        var areaNames = await db.Areas
            .AsNoTracking()
            .Where(a => areaIds.Contains(a.Id))
            .Select(a => new { a.Id, a.Name })
            .ToDictionaryAsync(x => x.Id, x => x.Name, ct);

        return routes.Select(r => new UserRouteInfo(r.AreaId, areaNames[r.AreaId], r.ClientId, r.ReturnUrl, r.Priority, r.IsActive)).ToList();
    }
}

public readonly record struct UserRouteInfo(
    int AreaId,
    string AreaName,
    string ClientId,
    string ReturnUrl,
    int Priority,
    bool IsActive);
