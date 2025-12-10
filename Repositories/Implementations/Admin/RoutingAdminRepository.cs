using Auth.Web.Application.Admin.Dtos;
using Auth.Web.Data;
using Auth.Web.Data.Entities;
using Auth.Web.Repositories.Abstractions.Admin;
using Microsoft.EntityFrameworkCore;

namespace Auth.Web.Repositories.Implementations.Admin;

public sealed class RoutingAdminRepository : IRoutingAdminRepository
{
    private readonly AuthDbContext _db;

    public RoutingAdminRepository(AuthDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyCollection<AreaRouteAdminDto>> GetRoutesAsync(CancellationToken ct = default)
    {
        var routes = await _db.AreaRoutes.AsNoTracking().ToListAsync(ct);
        var areas = await _db.Areas.AsNoTracking().ToListAsync(ct);
        var clients = await _db.ApplicationClients.AsNoTracking().ToListAsync(ct);
        return routes.Select(r => Map(r, areas, clients)).ToList();
    }

    public async Task<AreaRouteAdminDto?> GetRouteAsync(int id, CancellationToken ct = default)
    {
        var r = await _db.AreaRoutes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r is null) return null;
        var areas = await _db.Areas.AsNoTracking().ToListAsync(ct);
        var clients = await _db.ApplicationClients.AsNoTracking().ToListAsync(ct);
        return Map(r, areas, clients);
    }

    public async Task<int> CreateRouteAsync(int areaId, int clientId, string returnUrl, int priority, bool isActive, CancellationToken ct = default)
    {
        if (!await _db.Areas.AnyAsync(a => a.Id == areaId, ct)) throw new InvalidOperationException("Área inexistente.");
        var client = await _db.ApplicationClients.FindAsync(new object[] { clientId }, ct) ?? throw new InvalidOperationException("Cliente inexistente.");
        var entity = new AreaRoute { AreaId = areaId, ClientId = client.ClientId, ReturnUrl = returnUrl, Priority = priority, IsActive = isActive };
        _db.AreaRoutes.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task UpdateRouteAsync(int id, int areaId, int clientId, string returnUrl, int priority, bool isActive, CancellationToken ct = default)
    {
        var entity = await _db.AreaRoutes.FindAsync(new object[] { id }, ct) ?? throw new KeyNotFoundException("Ruta no encontrada.");
        if (!await _db.Areas.AnyAsync(a => a.Id == areaId, ct)) throw new InvalidOperationException("Área inexistente.");
        var client = await _db.ApplicationClients.FindAsync(new object[] { clientId }, ct) ?? throw new InvalidOperationException("Cliente inexistente.");
        entity.AreaId = areaId;
        entity.ClientId = client.ClientId;
        entity.ReturnUrl = returnUrl;
        entity.Priority = priority;
        entity.IsActive = isActive;
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteRouteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.AreaRoutes.FindAsync(new object[] { id }, ct);
        if (entity is null) return;
        _db.AreaRoutes.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    private static AreaRouteAdminDto Map(AreaRoute r, List<Area> areas, List<ApplicationClient> clients)
    {
        var area = areas.FirstOrDefault(a => a.Id == r.AreaId);
        var client = clients.FirstOrDefault(c => c.ClientId == r.ClientId);
        return new AreaRouteAdminDto
        {
            Id = r.Id,
            AreaId = r.AreaId,
            ClientId = client?.Id ?? 0,
            ClientIdentifier = r.ClientId,
            ReturnUrl = r.ReturnUrl,
            Priority = r.Priority,
            IsActive = r.IsActive,
            AreaName = area?.Name,
            ApplicationName = client?.ClientId
        };
    }
}
