using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Application.Admin.Dtos;
using Auth.Web.Services.Abstractions.Clients;
using Auth.Web.Repositories.Abstractions.Admin;

namespace Auth.Web.Services.Implementations.Admin;

public sealed class ClientAdminService : IAdminClientService
{
    private readonly IClientAdminRepository _repository;
    private readonly IClientService _clientService;

    public ClientAdminService(IClientAdminRepository repository, IClientService clientService)
    {
        _repository = repository;
        _clientService = clientService;
    }

    public Task<IReadOnlyCollection<ApplicationClientAdminDto>> GetClientsAsync(CancellationToken cancellationToken = default)
        => _repository.GetClientsAsync(cancellationToken);

    public Task<ApplicationClientAdminDto?> GetClientAsync(int id, CancellationToken cancellationToken = default)
        => _repository.GetClientAsync(id, cancellationToken);

    public Task<int> CreateClientAsync(string clientId, string audience, IEnumerable<string> allowedUrls, CancellationToken cancellationToken = default)
        => _repository.CreateClientAsync(clientId, audience, allowedUrls, cancellationToken);

    public Task UpdateClientAsync(int id, string clientId, string audience, IEnumerable<string> allowedUrls, CancellationToken cancellationToken = default)
        => _repository.UpdateClientAsync(id, clientId, audience, allowedUrls, cancellationToken);

    public Task DeleteClientAsync(int id, CancellationToken cancellationToken = default)
        => _repository.DeleteClientAsync(id, cancellationToken);
}
