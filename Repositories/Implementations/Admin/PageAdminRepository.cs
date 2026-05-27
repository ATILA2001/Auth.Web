using Auth.Web.Data;
using Auth.Web.Data.Entities;
using Auth.Web.Repositories.Abstractions.Admin;
using Microsoft.EntityFrameworkCore;

namespace Auth.Web.Repositories.Implementations.Admin;

public sealed class PageAdminRepository : IPageAdminRepository
{
    private readonly IDbContextFactory<AuthDbContext> _dbFactory;

    public PageAdminRepository(IDbContextFactory<AuthDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<Page>> GetPagesAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.Pages
            .AsNoTracking()
            .Include(p => p.Client)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);
    }

    public async Task<Page?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.Pages
            .AsNoTracking()
            .Include(p => p.Client)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<Page> CreateAsync(string name, string url, int? clientId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var page = new Page { Name = name.Trim(), Url = url.Trim(), ClientId = clientId };
        db.Pages.Add(page);
        await db.SaveChangesAsync(ct);
        return page;
    }

    public async Task<bool> UpdateAsync(int id, string name, string url, int? clientId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var page = await db.Pages.FindAsync(new object[] { id }, ct);
        if (page is null) return false;
        page.Name = name.Trim();
        page.Url = url.Trim();
        page.ClientId = clientId;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var page = await db.Pages.FindAsync(new object[] { id }, ct);
        if (page is null) return false;
        db.Pages.Remove(page);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyDictionary<int, int>> GetPagePermissionCountsAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var counts = await db.AreaPagePermissions.AsNoTracking()
            .GroupBy(a => a.PageId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(ct);
        return counts
            .Where(x => x.Key.HasValue)
            .ToDictionary(x => x.Key!.Value, x => x.Count);
    }

    public async Task<IReadOnlyDictionary<int, int>> GetPageAreaCountsAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var counts = await db.AreaPagePermissions.AsNoTracking()
            .Where(a => a.PageId.HasValue)
            .GroupBy(a => a.PageId)
            .Select(g => new { g.Key, Count = g.Select(a => a.AreaId).Distinct().Count() })
            .ToListAsync(ct);

        return counts
            .Where(x => x.Key.HasValue)
            .ToDictionary(x => x.Key!.Value, x => x.Count);
    }

    public async Task<int> GetPagePermissionCountAsync(int pageId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.AreaPagePermissions.CountAsync(a => a.PageId == pageId, ct);
    }

}
