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
}
