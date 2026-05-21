using System.Text.Json;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Application.Admin.Dtos;
using Auth.Web.Services.Abstractions.Clients;
using Auth.Web.Repositories.Abstractions.Admin;
using Auth.Web.Data.Entities;

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

    public async Task<IReadOnlyCollection<ApplicationClientAdminDto>> GetClientsAsync(CancellationToken cancellationToken = default)
    {
        var clients = await _repository.GetClientsAsync(cancellationToken);
        return clients.Select(MapToDto).ToList();
    }

    public async Task<ApplicationClientAdminDto?> GetClientAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetClientAsync(id, cancellationToken);
        return entity is null ? null : MapToDto(entity);
    }

    public Task<int> CreateClientAsync(string clientId, string audience, IEnumerable<string> allowedUrls, string? defaultLandingPage, CancellationToken cancellationToken = default)
        => _repository.CreateClientAsync(clientId, audience, allowedUrls, defaultLandingPage, cancellationToken);

    public Task UpdateClientAsync(int id, string clientId, string audience, IEnumerable<string> allowedUrls, string? defaultLandingPage, CancellationToken cancellationToken = default)
        => _repository.UpdateClientAsync(id, clientId, audience, allowedUrls, defaultLandingPage, cancellationToken);

    public Task DeleteClientAsync(int id, CancellationToken cancellationToken = default)
        => _repository.DeleteClientAsync(id, cancellationToken);

    private static ApplicationClientAdminDto MapToDto(ApplicationClient client)
        => new()
        {
            Id = client.Id,
            ClientId = client.ClientId,
            Audience = client.Audience,
            AllowedReturnUrls = DeserializeAllowedUrls(client.AllowedReturnUrlsJson),
            DefaultLandingPage = client.DefaultLandingPage
        };

    private static List<string> DeserializeAllowedUrls(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<string>();
        }
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}
