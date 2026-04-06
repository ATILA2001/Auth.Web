using Auth.Web.Data;
using Auth.Web.Data.Entities;
using Auth.Web.Repositories.Abstractions.Admin;
using Microsoft.EntityFrameworkCore;

namespace Auth.Web.Repositories.Implementations.Admin;

public sealed class AreaAdminRepository : IAreaAdminRepository
{
    private readonly IDbContextFactory<AuthDbContext> _dbFactory;

    public AreaAdminRepository(IDbContextFactory<AuthDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<Area>> GetAreasAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.Areas.AsNoTracking().OrderBy(a => a.Name).ToListAsync(ct);
    }

    public async Task<Area?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.Areas.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task<Area> CreateAsync(string name, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var area = new Area { Name = name.Trim() };
        db.Areas.Add(area);
        await db.SaveChangesAsync(ct);
        return area;
    }

    public async Task<bool> UpdateNameAsync(int id, string name, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var area = await db.Areas.FindAsync(new object[] { id }, ct);
        if (area is null) return false;
        area.Name = name.Trim();
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var area = await db.Areas.FindAsync(new object[] { id }, ct);
        if (area is null) return false;
        db.Areas.Remove(area);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyDictionary<int, int>> GetAreaUserCountsAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var counts = await db.UserAreas.AsNoTracking()
            .GroupBy(ua => ua.AreaId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(ct);
        return counts.ToDictionary(x => x.Key, x => x.Count);
    }

    public async Task<int> GetAreaUserCountAsync(int areaId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.UserAreas.CountAsync(ua => ua.AreaId == areaId, ct);
    }

    public async Task<IReadOnlyDictionary<int, (int ClientId, string Audience)>> GetAreaClientMappingAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var routes = await db.AreaRoutes
            .AsNoTracking()
            .Where(r => r.ClientId != null && r.AreaId != null)
            .Join(db.ApplicationClients,
                r => r.ClientId,
                c => c.Id,
                (r, c) => new { AreaId = r.AreaId!.Value, ClientId = r.ClientId!.Value, c.Audience })
            .ToListAsync(ct);
        return routes
            .GroupBy(r => r.AreaId)
            .ToDictionary(
                g => g.Key,
                g => (g.First().ClientId, g.First().Audience));
    }
}
