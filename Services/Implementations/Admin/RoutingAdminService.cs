using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Application.Admin.Dtos;
using Auth.Web.Services.Abstractions.Clients;
using Auth.Web.Data;
using Auth.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.Web.Services.Implementations.Admin;

public sealed class RoutingAdminService : IAdminRoutingService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IClientService _clientService;

    public RoutingAdminService(IServiceScopeFactory scopeFactory, IClientService clientService)
    {
        _scopeFactory = scopeFactory;
        _clientService = clientService;
    }

    public async Task<IReadOnlyCollection<AreaRouteAdminDto>> GetRoutesAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var routes = await db.AreaRoutes.AsNoTracking().ToListAsync(cancellationToken);
        var areas = await db.Areas.AsNoTracking().ToListAsync(cancellationToken);
        var clients = await db.ApplicationClients.AsNoTracking().ToListAsync(cancellationToken);
        return routes.Select(r => Map(r, areas, clients)).ToList();
    }

    public async Task<AreaRouteAdminDto?> GetRouteAsync(int id, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var r = await db.AreaRoutes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (r is null) return null;
        var areas = await db.Areas.AsNoTracking().ToListAsync(cancellationToken);
        var clients = await db.ApplicationClients.AsNoTracking().ToListAsync(cancellationToken);
        return Map(r, areas, clients);
    }

    public async Task<int> CreateRouteAsync(int areaId, int clientId, string returnUrl, int priority, bool isActive, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        if (!await db.Areas.AnyAsync(a => a.Id == areaId, cancellationToken)) throw new InvalidOperationException("Área inexistente.");
        var client = await db.ApplicationClients.FindAsync(new object[] { clientId }, cancellationToken) ?? throw new InvalidOperationException("Cliente inexistente.");
        if (!_clientService.IsReturnUrlAllowed(client, returnUrl)) throw new InvalidOperationException("ReturnUrl no permitido para el cliente.");
        var entity = new AreaRoute { AreaId = areaId, ClientId = client.ClientId, ReturnUrl = returnUrl, Priority = priority, IsActive = isActive };
        db.AreaRoutes.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    public async Task UpdateRouteAsync(int id, int areaId, int clientId, string returnUrl, int priority, bool isActive, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var entity = await db.AreaRoutes.FindAsync(new object[] { id }, cancellationToken) ?? throw new KeyNotFoundException("Ruta no encontrada.");
        if (!await db.Areas.AnyAsync(a => a.Id == areaId, cancellationToken)) throw new InvalidOperationException("Área inexistente.");
        var client = await db.ApplicationClients.FindAsync(new object[] { clientId }, cancellationToken) ?? throw new InvalidOperationException("Cliente inexistente.");
        if (!_clientService.IsReturnUrlAllowed(client, returnUrl)) throw new InvalidOperationException("ReturnUrl no permitido para el cliente.");
        entity.AreaId = areaId;
        entity.ClientId = client.ClientId;
        entity.ReturnUrl = returnUrl;
        entity.Priority = priority;
        entity.IsActive = isActive;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteRouteAsync(int id, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var entity = await db.AreaRoutes.FindAsync(new object[] { id }, cancellationToken);
        if (entity is null) return;
        db.AreaRoutes.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
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
