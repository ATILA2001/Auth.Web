using Auth.Web.Data;
using Auth.Web.Data.Entities;
using Auth.Web.Repositories.Abstractions.Admin;
using Microsoft.EntityFrameworkCore;

namespace Auth.Web.Repositories.Implementations.Admin;

public sealed class AreaAdminRepository : IAreaAdminRepository
{
    private readonly AuthDbContext _db;

    public AreaAdminRepository(AuthDbContext db)
    {
        _db = db;
    }

    public Task<List<Area>> GetAreasAsync(CancellationToken ct = default)
        => _db.Areas.AsNoTracking().OrderBy(a => a.Name).ToListAsync(ct);

    public Task<Area?> GetByIdAsync(int id, CancellationToken ct = default)
        => _db.Areas.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<Area> CreateAsync(string name, CancellationToken ct = default)
    {
        var area = new Area { Name = name.Trim() };
        _db.Areas.Add(area);
        await _db.SaveChangesAsync(ct);
        return area;
    }

    public async Task<bool> UpdateNameAsync(int id, string name, CancellationToken ct = default)
    {
        var area = await _db.Areas.FindAsync(new object[] { id }, ct);
        if (area is null) return false;
        area.Name = name.Trim();
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var area = await _db.Areas.FindAsync(new object[] { id }, ct);
        if (area is null) return false;
        _db.Areas.Remove(area);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyDictionary<int, int>> GetAreaUserCountsAsync(CancellationToken ct = default)
    {
        var counts = await _db.UserAreas.AsNoTracking()
            .GroupBy(ua => ua.AreaId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(ct);
        return counts.ToDictionary(x => x.Key, x => x.Count);
    }

    public Task<int> GetAreaUserCountAsync(int areaId, CancellationToken ct = default)
        => _db.UserAreas.CountAsync(ua => ua.AreaId == areaId, ct);
}
