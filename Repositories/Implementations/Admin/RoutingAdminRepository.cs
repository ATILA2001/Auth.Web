using Auth.Web.Data;
using Auth.Web.Data.Entities;
using Auth.Web.Repositories.Abstractions.Admin;
using Microsoft.EntityFrameworkCore;

namespace Auth.Web.Repositories.Implementations.Admin;

public sealed class RoutingAdminRepository : IRoutingAdminRepository
{
    private readonly IDbContextFactory<AuthDbContext> _dbFactory;

    public RoutingAdminRepository(IDbContextFactory<AuthDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IReadOnlyCollection<AreaRoute>> GetRoutesAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.AreaRoutes.AsNoTracking().ToListAsync(ct);
    }

    public async Task<AreaRoute?> GetRouteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.AreaRoutes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<int> CreateRouteAsync(int areaId, int clientId, string returnUrl, int priority, bool isActive, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        if (!await db.Areas.AnyAsync(a => a.Id == areaId, ct)) throw new InvalidOperationException("Area inexistente.");
        var client = await db.ApplicationClients.FindAsync(new object[] { clientId }, ct) ?? throw new InvalidOperationException("Cliente inexistente.");
        var entity = new AreaRoute { AreaId = areaId, ClientId = client.ClientId, ReturnUrl = returnUrl, Priority = priority, IsActive = isActive };
        db.AreaRoutes.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task UpdateRouteAsync(int id, int areaId, int clientId, string returnUrl, int priority, bool isActive, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var entity = await db.AreaRoutes.FindAsync(new object[] { id }, ct) ?? throw new KeyNotFoundException("Ruta no encontrada.");
        if (!await db.Areas.AnyAsync(a => a.Id == areaId, ct)) throw new InvalidOperationException("Area inexistente.");
        var client = await db.ApplicationClients.FindAsync(new object[] { clientId }, ct) ?? throw new InvalidOperationException("Cliente inexistente.");
        entity.AreaId = areaId;
        entity.ClientId = client.ClientId;
        entity.ReturnUrl = returnUrl;
        entity.Priority = priority;
        entity.IsActive = isActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteRouteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var entity = await db.AreaRoutes.FindAsync(new object[] { id }, ct);
        if (entity is null) return;
        db.AreaRoutes.Remove(entity);
        await db.SaveChangesAsync(ct);
    }
}
