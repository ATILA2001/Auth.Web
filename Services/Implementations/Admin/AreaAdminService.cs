using Microsoft.EntityFrameworkCore;
using Auth.Web.Data;
using Auth.Web.Domain.Entities;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Application.Admin.Dtos;

namespace Auth.Web.Services.Implementations.Admin;

public sealed class AreaAdminService : IAdminAreaService
{
    private readonly IServiceScopeFactory _scopeFactory;
    
    public AreaAdminService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<List<Area>> GetAreasAsync(CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        return await db.Areas.AsNoTracking().OrderBy(a => a.Name).ToListAsync(ct);
    }

    public async Task<Area?> CreateAsync(string name, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        
        var area = new Area { Name = name.Trim() };
        db.Areas.Add(area);
        await db.SaveChangesAsync(ct);
        return area;
    }

    public async Task<bool> UpdateNameAsync(int id, string name, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        
        var area = await db.Areas.FindAsync(new object[] { id }, ct);
        if (area is null) return false;
        area.Name = name.Trim();
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        
        var area = await db.Areas.FindAsync(new object[] { id }, ct);
        if (area is null) return false;
        db.Areas.Remove(area);
        await db.SaveChangesAsync(ct);
        return true;
    }

    async Task<IReadOnlyCollection<AreaAdminDto>> IAdminAreaService.GetAreasAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        
        var areas = await db.Areas.AsNoTracking().OrderBy(a => a.Name).ToListAsync(cancellationToken);
        var counts = await db.UserAreas.AsNoTracking().GroupBy(ua => ua.AreaId)
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
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        
        var area = await db.Areas.AsNoTracking().FirstOrDefaultAsync(a => a.Id == areaId, cancellationToken);
        if (area is null) return null;
        var count = await db.UserAreas.CountAsync(ua => ua.AreaId == areaId, cancellationToken);
        return new AreaAdminDto { Id = area.Id, Name = area.Name, UserCount = count };
    }

    async Task<int> IAdminAreaService.CreateAreaAsync(string name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name)) return 0;
        
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        
        var area = new Area { Name = name.Trim() };
        db.Areas.Add(area);
        await db.SaveChangesAsync(cancellationToken);
        return area.Id;
    }

    async Task IAdminAreaService.UpdateAreaAsync(int areaId, string name, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        
        var area = await db.Areas.FindAsync(new object[] { areaId }, cancellationToken);
        if (area is null) return;
        area.Name = name.Trim();
        await db.SaveChangesAsync(cancellationToken);
    }

    async Task IAdminAreaService.DeleteAreaAsync(int areaId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        
        var area = await db.Areas.FindAsync(new object[] { areaId }, cancellationToken);
        if (area is null) return;
        db.Areas.Remove(area);
        await db.SaveChangesAsync(cancellationToken);
    }
}
