using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Application.Admin.Dtos;
using Auth.Web.Services.Abstractions.Clients;
using Auth.Web.Repositories.Abstractions.Admin;
using Auth.Web.Repositories.Abstractions;
using Auth.Web.Data.Entities;

namespace Auth.Web.Services.Implementations.Admin;

public sealed class RoutingAdminService : IAdminRoutingService
{
    private readonly IRoutingAdminRepository _repository;
    private readonly IClientService _clientService;
    private readonly IClientAdminRepository _clientAdminRepository;
    private readonly IAreaRepository _areaRepository;

    public RoutingAdminService(IRoutingAdminRepository repository, IClientService clientService, IClientAdminRepository clientAdminRepository, IAreaRepository areaRepository)
    {
        _repository = repository;
        _clientService = clientService;
        _clientAdminRepository = clientAdminRepository;
        _areaRepository = areaRepository;
    }

    public async Task<IReadOnlyCollection<AreaRouteAdminDto>> GetRoutesAsync(CancellationToken cancellationToken = default)
    {
        var routes = await _repository.GetRoutesAsync(cancellationToken);
        if (routes.Count == 0)
        {
            return Array.Empty<AreaRouteAdminDto>();
        }

        var areaNames = await _areaRepository.GetAreaNamesAsync(cancellationToken);
        var clients = await _clientAdminRepository.GetClientsAsync(cancellationToken);
        var clientMap = clients.ToDictionary(c => c.Id, c => c);

        return routes.Select(r => MapRoute(r,
            r.AreaId.HasValue && areaNames.TryGetValue(r.AreaId.Value, out var areaName) ? areaName : null,
            r.ClientId.HasValue && clientMap.TryGetValue(r.ClientId.Value, out var client) ? client : null)).ToList();
    }

    public async Task<AreaRouteAdminDto?> GetRouteAsync(int id, CancellationToken cancellationToken = default)
    {
        var route = await _repository.GetRouteAsync(id, cancellationToken);
        if (route is null)
        {
            return null;
        }

        var areaName = route.AreaId.HasValue
            ? await _areaRepository.GetAreaNameAsync(route.AreaId.Value, cancellationToken)
            : null;
        var client = route.ClientId.HasValue
            ? await _clientAdminRepository.GetClientAsync(route.ClientId.Value, cancellationToken)
            : null;
        return MapRoute(route, areaName, client);
    }

    public async Task<int> CreateRouteAsync(int areaId, int clientId, int priority, bool isActive, CancellationToken cancellationToken = default)
    {
        var clientEntity = await _clientAdminRepository.GetClientAsync(clientId, cancellationToken) ?? throw new InvalidOperationException("Cliente inexistente.");
        var domainClient = await _clientService.GetAsync(clientEntity.ClientId);
        if (domainClient is null)
        {
            throw new InvalidOperationException("Cliente inexistente.");
        }
        return await _repository.CreateRouteAsync(areaId, clientId, priority, isActive, cancellationToken);
    }

    public async Task UpdateRouteAsync(int id, int areaId, int clientId, int priority, bool isActive, CancellationToken cancellationToken = default)
    {
        var clientEntity = await _clientAdminRepository.GetClientAsync(clientId, cancellationToken) ?? throw new InvalidOperationException("Cliente inexistente.");
        var domainClient = await _clientService.GetAsync(clientEntity.ClientId);
        if (domainClient is null)
        {
            throw new InvalidOperationException("Cliente inexistente.");
        }
        await _repository.UpdateRouteAsync(id, areaId, clientId, priority, isActive, cancellationToken);
    }

    public Task DeleteRouteAsync(int id, CancellationToken cancellationToken = default)
        => _repository.DeleteRouteAsync(id, cancellationToken);

    private static AreaRouteAdminDto MapRoute(AreaRoute route, string? areaName, ApplicationClient? client)
        => new()
        {
            Id = route.Id,
            AreaId = route.AreaId,
            ClientId = client?.Id,
            ClientIdentifier = client?.ClientId ?? string.Empty,
            Priority = route.Priority,
            IsActive = route.IsActive,
            AreaName = areaName ?? "Sin asignar",
            ApplicationName = client?.ClientId ?? "Sin asignar"
        };
}
