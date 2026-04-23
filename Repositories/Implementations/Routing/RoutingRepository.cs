using Auth.Web.Data;
using Auth.Web.Data.Entities;
using Auth.Web.Repositories.Abstractions.Routing;
using Microsoft.EntityFrameworkCore;

namespace Auth.Web.Repositories.Implementations.Routing;

public sealed class RoutingRepository : IRoutingRepository
{
    private readonly AuthDbContext _db;

    public RoutingRepository(AuthDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyCollection<int>> GetUserAreaIdsAsync(string userId, CancellationToken ct = default)
    {
        return await _db.UserAreas
            .AsNoTracking()
            .Where(ua => ua.UserId == userId)
            .Select(ua => ua.AreaId)
            .ToListAsync(ct);
    }

    public async Task<AreaRoute?> GetFirstActiveRouteForAreasAsync(IEnumerable<int> areaIds, CancellationToken ct = default)
    {
        var ids = areaIds?.ToArray() ?? Array.Empty<int>();
        if (ids.Length == 0) return null;

        return await _db.AreaRoutes
            .AsNoTracking()
            .Where(r => r.IsActive && r.AreaId.HasValue && r.ClientId.HasValue && ids.Contains(r.AreaId.Value))
            .Include(r => r.Client)
            .OrderBy(r => r.Priority)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<AreaRoute>> GetDistinctActiveRoutesForAreasAsync(IEnumerable<int> areaIds, CancellationToken ct = default)
    {
        var ids = areaIds?.ToArray() ?? Array.Empty<int>();
        if (ids.Length == 0) return [];

        var allRoutes = await _db.AreaRoutes
            .AsNoTracking()
            .Where(r => r.IsActive && r.AreaId.HasValue && r.ClientId.HasValue && ids.Contains(r.AreaId.Value))
            .Include(r => r.Client)
            .OrderBy(r => r.Priority)
            .ToListAsync(ct);

        // One route per distinct ClientId (the one with lowest Priority value wins)
        return allRoutes
            .GroupBy(r => r.ClientId)
            .Select(g => g.First())
            .ToList();
    }

    public async Task<IReadOnlyDictionary<string, int>> GetDefaultAreaIdPerClientAsync(IEnumerable<string> clientIds, CancellationToken ct = default)
    {
        var ids = clientIds?.ToArray() ?? Array.Empty<string>();
        if (ids.Length == 0) return new Dictionary<string, int>();

        var routes = await _db.AreaRoutes
            .AsNoTracking()
            .Where(r => r.IsActive && r.AreaId.HasValue && r.ClientId.HasValue)
            .Include(r => r.Client)
            .Where(r => r.Client != null && ids.Contains(r.Client.ClientId))
            .OrderBy(r => r.Priority)
            .ToListAsync(ct);

        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var route in routes)
        {
            if (route.Client?.ClientId is not null && route.AreaId.HasValue
                && !result.ContainsKey(route.Client.ClientId))
            {
                result[route.Client.ClientId] = route.AreaId.Value;
            }
        }
        return result;
    }

    public async Task<IReadOnlyDictionary<int, string>> GetClientIdByAreaIdAsync(IEnumerable<int> areaIds, CancellationToken ct = default)
    {
        var ids = areaIds?.ToArray() ?? Array.Empty<int>();
        if (ids.Length == 0) return new Dictionary<int, string>();

        var routes = await _db.AreaRoutes
            .AsNoTracking()
            .Where(r => r.IsActive && r.AreaId.HasValue && r.ClientId.HasValue && ids.Contains(r.AreaId.Value))
            .Include(r => r.Client)
            .OrderBy(r => r.Priority)
            .ToListAsync(ct);

        var result = new Dictionary<int, string>();
        foreach (var route in routes)
        {
            if (route.AreaId.HasValue && route.Client?.ClientId is not null
                && !result.ContainsKey(route.AreaId.Value))
            {
                result[route.AreaId.Value] = route.Client.ClientId;
            }
        }
        return result;
    }
}
