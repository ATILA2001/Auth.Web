using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Application.Admin.Dtos;
using Auth.Web.Services.Abstractions.Clients;
using Auth.Web.Repositories.Abstractions.Admin;
using Auth.Web.Domain.Entities;

namespace Auth.Web.Services.Implementations.Admin;

public sealed class RoutingAdminService : IAdminRoutingService
{
    private readonly IRoutingAdminRepository _repository;
    private readonly IClientService _clientService;
    private readonly IClientAdminRepository _clientAdminRepository;

    public RoutingAdminService(IRoutingAdminRepository repository, IClientService clientService, IClientAdminRepository clientAdminRepository)
    {
        _repository = repository;
        _clientService = clientService;
        _clientAdminRepository = clientAdminRepository;
    }

    public Task<IReadOnlyCollection<AreaRouteAdminDto>> GetRoutesAsync(CancellationToken cancellationToken = default)
        => _repository.GetRoutesAsync(cancellationToken);

    public Task<AreaRouteAdminDto?> GetRouteAsync(int id, CancellationToken cancellationToken = default)
        => _repository.GetRouteAsync(id, cancellationToken);

    public async Task<int> CreateRouteAsync(int areaId, int clientId, string returnUrl, int priority, bool isActive, CancellationToken cancellationToken = default)
    {
        // Ensure client exists via repository
        var clientDto = await _clientAdminRepository.GetClientAsync(clientId, cancellationToken) ?? throw new InvalidOperationException("Cliente inexistente.");
        // Load domain client entity directly from client service by string identifier
        var domainClient = await _clientService.GetAsync(clientDto.ClientId);
        if (domainClient is null)
        {
            throw new InvalidOperationException("Cliente inexistente.");
        }
        // Validate return url via IsReturnUrlAllowed on hydrated domain client entity
        if (!_clientService.IsReturnUrlAllowed(domainClient, returnUrl))
        {
            // However repository will persist any value, so guard here ensures consistency
            throw new InvalidOperationException("ReturnUrl no permitido para el cliente.");
        }
        // Repository persists with numeric clientId and stores ClientId (string) internally
        return await _repository.CreateRouteAsync(areaId, clientId, returnUrl, priority, isActive, cancellationToken);
    }

    public async Task UpdateRouteAsync(int id, int areaId, int clientId, string returnUrl, int priority, bool isActive, CancellationToken cancellationToken = default)
    {
        var clientDto = await _clientAdminRepository.GetClientAsync(clientId, cancellationToken) ?? throw new InvalidOperationException("Cliente inexistente.");
        var domainClient = await _clientService.GetAsync(clientDto.ClientId);
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
}
