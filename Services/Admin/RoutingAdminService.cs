using Microsoft.EntityFrameworkCore;
using Auth.Web.Data;
using Auth.Web.Domain.Entities;

namespace Auth.Web.Services.Admin;

public interface IRoutingAdminService
{
    Task<(List<Area> Areas, List<AreaRoute> Rules)> GetAsync(CancellationToken ct = default);
    Task<AreaRoute?> CreateAsync(AreaRoute rule, CancellationToken ct = default);
    Task<bool> UpdateAsync(AreaRoute rule, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}

public sealed class RoutingAdminService : IRoutingAdminService
{
    private readonly IServiceScopeFactory _scopeFactory;
    public RoutingAdminService(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public async Task<(List<Area> Areas, List<AreaRoute> Rules)> GetAsync(CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var areas = await db.Areas.AsNoTracking().OrderBy(a => a.Name).ToListAsync(ct);
        var rules = await db.AreaRoutes.AsNoTracking().OrderBy(r => r.Priority).ToListAsync(ct);
        return (areas, rules);
    }

    public async Task<AreaRoute?> CreateAsync(AreaRoute rule, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        db.AreaRoutes.Add(rule);
        await db.SaveChangesAsync(ct);
        return rule;
    }

    public async Task<bool> UpdateAsync(AreaRoute rule, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var existing = await db.AreaRoutes.FindAsync(new object[] { rule.Id }, ct);
        if (existing is null) return false;
        existing.AreaId = rule.AreaId;
        existing.ClientId = rule.ClientId.Trim();
        existing.ReturnUrl = rule.ReturnUrl.Trim();
        existing.Priority = rule.Priority;
        existing.IsActive = rule.IsActive;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var existing = await db.AreaRoutes.FindAsync(new object[] { id }, ct);
        if (existing is null) return false;
        db.AreaRoutes.Remove(existing);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
