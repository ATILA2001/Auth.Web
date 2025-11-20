using Auth.Web.Application.Admin.Abstractions;
using Auth.Web.Application.Admin.Dtos;
using Auth.Web.Application.Abstractions; // IClientService abstraction
using Auth.Web.Data;
using Auth.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Auth.Web.Infrastructure.Admin;

public sealed class RoutingAdminService : IAdminRoutingService
{
    private readonly AuthDbContext _db;
    private readonly IClientService _clientService;

    public RoutingAdminService(AuthDbContext db, IClientService clientService)
    {
        _db = db;
        _clientService = clientService;
    }

    public async Task<IReadOnlyCollection<AreaRouteAdminDto>> GetRoutesAsync(CancellationToken cancellationToken = default)
    {
        var areas = await _db.Areas.AsNoTracking().ToListAsync(cancellationToken);
        var clients = await _db.ApplicationClients.AsNoTracking().ToListAsync(cancellationToken);
        var routes = await _db.AreaRoutes.AsNoTracking().OrderBy(r => r.Priority).ToListAsync(cancellationToken);
        return routes.Select(r => Map(r, areas, clients)).ToList();
    }

    public async Task<AreaRouteAdminDto?> GetRouteAsync(int id, CancellationToken cancellationToken = default)
    {
        var areas = await _db.Areas.AsNoTracking().ToListAsync(cancellationToken);
        var clients = await _db.ApplicationClients.AsNoTracking().ToListAsync(cancellationToken);
        var route = await _db.AreaRoutes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        return route is null ? null : Map(route, areas, clients);
    }

    public async Task<int> CreateRouteAsync(int areaId, int clientId, string returnUrl, int priority, bool isActive, CancellationToken cancellationToken = default)
    {
        var area = await _db.Areas.FindAsync(new object[] { areaId }, cancellationToken) ?? throw new KeyNotFoundException("Área no encontrada.");
        var client = await _db.ApplicationClients.FindAsync(new object[] { clientId }, cancellationToken) ?? throw new KeyNotFoundException("Cliente no encontrado.");
        if (!_clientService.IsReturnUrlAllowed(client, returnUrl))
        {
            throw new InvalidOperationException("ReturnUrl no permitida para el cliente.");
        }
        var entity = new AreaRoute
        {
            AreaId = area.Id,
            ClientId = client.ClientId,
            ReturnUrl = returnUrl.Trim(),
            Priority = priority,
            IsActive = isActive
        };
        _db.AreaRoutes.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    public async Task UpdateRouteAsync(int id, int areaId, int clientId, string returnUrl, int priority, bool isActive, CancellationToken cancellationToken = default)
    {
        var entity = await _db.AreaRoutes.FindAsync(new object[] { id }, cancellationToken) ?? throw new KeyNotFoundException("Ruta no encontrada.");
        var area = await _db.Areas.FindAsync(new object[] { areaId }, cancellationToken) ?? throw new KeyNotFoundException("Área no encontrada.");
        var client = await _db.ApplicationClients.FindAsync(new object[] { clientId }, cancellationToken) ?? throw new KeyNotFoundException("Cliente no encontrado.");
        if (!_clientService.IsReturnUrlAllowed(client, returnUrl))
        {
            throw new InvalidOperationException("ReturnUrl no permitida para el cliente.");
        }
        entity.AreaId = area.Id;
        entity.ClientId = client.ClientId;
        entity.ReturnUrl = returnUrl.Trim();
        entity.Priority = priority;
        entity.IsActive = isActive;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteRouteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.AreaRoutes.FindAsync(new object[] { id }, cancellationToken);
        if (entity is null) return;
        _db.AreaRoutes.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
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
