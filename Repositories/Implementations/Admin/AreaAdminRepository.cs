using Auth.Web.Application.Admin.Dtos;
using Auth.Web.Data;
using Auth.Web.Domain.Entities;
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

    public async Task<IReadOnlyCollection<AreaAdminDto>> GetAreasWithUserCountAsync(CancellationToken ct = default)
    {
        var areas = await _db.Areas.AsNoTracking().OrderBy(a => a.Name).ToListAsync(ct);
        var counts = await _db.UserAreas.AsNoTracking().GroupBy(ua => ua.AreaId)
            .Select(g => new { AreaId = g.Key, Count = g.Count() }).ToListAsync(ct);
        var map = counts.ToDictionary(x => x.AreaId, x => x.Count);
        return areas.Select(a => new AreaAdminDto
        {
            Id = a.Id,
            Name = a.Name,
            UserCount = map.TryGetValue(a.Id, out var c) ? c : 0
        }).ToList();
    }

    public async Task<AreaAdminDto?> GetAreaWithUserCountByIdAsync(int areaId, CancellationToken ct = default)
    {
        var area = await _db.Areas.AsNoTracking().FirstOrDefaultAsync(a => a.Id == areaId, ct);
        if (area is null) return null;
        var count = await _db.UserAreas.CountAsync(ua => ua.AreaId == areaId, ct);
        return new AreaAdminDto { Id = area.Id, Name = area.Name, UserCount = count };
    }
}
