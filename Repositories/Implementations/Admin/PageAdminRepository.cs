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
        return await db.Pages.AsNoTracking().OrderBy(p => p.Name).ToListAsync(ct);
    }

    public async Task<Page?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.Pages.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<Page> CreateAsync(string name, string url, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var page = new Page { Name = name.Trim(), Url = url.Trim() };
        db.Pages.Add(page);
        await db.SaveChangesAsync(ct);
        return page;
    }

    public async Task<bool> UpdateAsync(int id, string name, string url, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var page = await db.Pages.FindAsync(new object[] { id }, ct);
        if (page is null) return false;
        page.Name = name.Trim();
        page.Url = url.Trim();
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
        var counts = await db.RolePagePermissions.AsNoTracking()
            .GroupBy(rpp => rpp.PageId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(ct);
        return counts.ToDictionary(x => x.Key, x => x.Count);
    }

    public async Task<int> GetPagePermissionCountAsync(int pageId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.RolePagePermissions.CountAsync(rpp => rpp.PageId == pageId, ct);
    }
}
