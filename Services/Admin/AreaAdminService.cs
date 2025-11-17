using Microsoft.EntityFrameworkCore;
using Auth.Web.Data;
using Auth.Web.Domain.Entities;

namespace Auth.Web.Services.Admin;

public interface IAreaAdminService
{
    Task<List<Area>> GetAreasAsync(CancellationToken ct = default);
    Task<Area?> CreateAsync(string name, CancellationToken ct = default);
    Task<bool> UpdateNameAsync(int id, string name, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}

public sealed class AreaAdminService : IAreaAdminService
{
    private readonly IServiceScopeFactory _scopeFactory;
    public AreaAdminService(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

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
}
