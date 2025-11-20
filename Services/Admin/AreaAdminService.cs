using Microsoft.EntityFrameworkCore;
using Auth.Web.Data;
using Auth.Web.Domain.Entities;
using Auth.Web.Application.Admin.Abstractions;
using Auth.Web.Application.Admin.Dtos;

namespace Auth.Web.Services.Admin;

public sealed class AreaAdminService : IAdminAreaService
{
    private readonly AuthDbContext _db;
    public AreaAdminService(AuthDbContext db) => _db = db;

    // Legacy-compatible methods (used by existing UI)
    public async Task<List<Area>> GetAreasAsync(CancellationToken ct = default)
        => await _db.Areas.AsNoTracking().OrderBy(a => a.Name).ToListAsync(ct);

    public async Task<Area?> CreateAsync(string name, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
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

    // New interface explicit implementation
    async Task<IReadOnlyCollection<AreaAdminDto>> IAdminAreaService.GetAreasAsync(CancellationToken cancellationToken)
    {
        var areas = await _db.Areas.AsNoTracking().OrderBy(a => a.Name).ToListAsync(cancellationToken);
        var counts = await _db.UserAreas.AsNoTracking().GroupBy(ua => ua.AreaId)
            .Select(g => new { AreaId = g.Key, Count = g.Count() }).ToListAsync(cancellationToken);
        var map = counts.ToDictionary(x => x.AreaId, x => x.Count);
        return areas.Select(a => new AreaAdminDto
        {
            Id = a.Id,
            Name = a.Name,
            UserCount = map.TryGetValue(a.Id, out var c) ? c : 0
        }).ToList();
    }

    async Task<AreaAdminDto?> IAdminAreaService.GetAreaByIdAsync(int areaId, CancellationToken cancellationToken)
    {
        var area = await _db.Areas.AsNoTracking().FirstOrDefaultAsync(a => a.Id == areaId, cancellationToken);
        if (area is null) return null;
        var count = await _db.UserAreas.CountAsync(ua => ua.AreaId == areaId, cancellationToken);
        return new AreaAdminDto { Id = area.Id, Name = area.Name, UserCount = count };
    }

    async Task<int> IAdminAreaService.CreateAreaAsync(string name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name)) return 0;
        var area = new Area { Name = name.Trim() };
        _db.Areas.Add(area);
        await _db.SaveChangesAsync(cancellationToken);
        return area.Id;
    }

    async Task IAdminAreaService.UpdateAreaAsync(int areaId, string name, CancellationToken cancellationToken)
    {
        var area = await _db.Areas.FindAsync(new object[] { areaId }, cancellationToken);
        if (area is null) return;
        area.Name = name.Trim();
        await _db.SaveChangesAsync(cancellationToken);
    }

    async Task IAdminAreaService.DeleteAreaAsync(int areaId, CancellationToken cancellationToken)
    {
        var area = await _db.Areas.FindAsync(new object[] { areaId }, cancellationToken);
        if (area is null) return;
        _db.Areas.Remove(area);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
