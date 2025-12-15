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
        var clientMap = clients.ToDictionary(c => c.ClientId, c => c, StringComparer.OrdinalIgnoreCase);

        return routes.Select(r => MapRoute(r,
            areaNames.TryGetValue(r.AreaId, out var areaName) ? areaName : null,
            clientMap.TryGetValue(r.ClientId, out var client) ? client : null)).ToList();
    }

    public async Task<AreaRouteAdminDto?> GetRouteAsync(int id, CancellationToken cancellationToken = default)
    {
        var route = await _repository.GetRouteAsync(id, cancellationToken);
        if (route is null)
        {
            return null;
        }

        var areaName = await _areaRepository.GetAreaNameAsync(route.AreaId, cancellationToken);
        var client = await _clientService.GetAsync(route.ClientId);
        return MapRoute(route, areaName, client);
    }

    public async Task<int> CreateRouteAsync(int areaId, int clientId, string returnUrl, int priority, bool isActive, CancellationToken cancellationToken = default)
    {
        var clientEntity = await _clientAdminRepository.GetClientAsync(clientId, cancellationToken) ?? throw new InvalidOperationException("Cliente inexistente.");
        var domainClient = await _clientService.GetAsync(clientEntity.ClientId);
        if (domainClient is null)
        {
            throw new InvalidOperationException("Cliente inexistente.");
        }
        if (!_clientService.IsReturnUrlAllowed(domainClient, returnUrl))
        {
            throw new InvalidOperationException("ReturnUrl no permitido para el cliente.");
        }
        return await _repository.CreateRouteAsync(areaId, clientId, returnUrl, priority, isActive, cancellationToken);
    }

    public async Task UpdateRouteAsync(int id, int areaId, int clientId, string returnUrl, int priority, bool isActive, CancellationToken cancellationToken = default)
    {
        var clientEntity = await _clientAdminRepository.GetClientAsync(clientId, cancellationToken) ?? throw new InvalidOperationException("Cliente inexistente.");
        var domainClient = await _clientService.GetAsync(clientEntity.ClientId);
        if (domainClient is null)
        {
            throw new InvalidOperationException("Cliente inexistente.");
        }
        if (!_clientService.IsReturnUrlAllowed(domainClient, returnUrl))
        {
            throw new InvalidOperationException("ReturnUrl no permitido para el cliente.");
        }
        await _repository.UpdateRouteAsync(id, areaId, clientId, returnUrl, priority, isActive, cancellationToken);
    }

    public Task DeleteRouteAsync(int id, CancellationToken cancellationToken = default)
        => _repository.DeleteRouteAsync(id, cancellationToken);

    private static AreaRouteAdminDto MapRoute(AreaRoute route, string? areaName, ApplicationClient? client)
        => new()
        {
            Id = route.Id,
            AreaId = route.AreaId,
            ClientId = client?.Id ?? 0,
            ClientIdentifier = route.ClientId,
            ReturnUrl = route.ReturnUrl,
            Priority = route.Priority,
            IsActive = route.IsActive,
            AreaName = areaName,
            ApplicationName = client?.ClientId
        };
}
