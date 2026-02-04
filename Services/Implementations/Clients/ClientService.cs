using System.Text.Json;
using Auth.Web.Data.Entities;
using Auth.Web.Services.Abstractions.Clients;
using Auth.Web.Repositories.Abstractions.Clients;

namespace Auth.Web.Services.Implementations.Clients;

public class ClientService : IClientService
{
    private readonly IClientRepository _repository;

    public ClientService(IClientRepository repository)
    {
        _repository = repository;
    }

    public Task<ApplicationClient?> GetAsync(string clientId)
    {
        return _repository.GetAsync(clientId);
    }

    public bool IsReturnUrlAllowed(ApplicationClient client, string returnUrl)
    {
        if (string.IsNullOrWhiteSpace(client.AllowedReturnUrlsJson))
        {
            return false;
        }

        try
        {
            var urls = JsonSerializer.Deserialize<List<string>>(client.AllowedReturnUrlsJson) ?? [];
            return urls.Any(u => string.Equals(u?.TrimEnd('/'), returnUrl?.TrimEnd('/'), StringComparison.OrdinalIgnoreCase));
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public string? GetDefaultReturnUrl(ApplicationClient client)
    {
        if (string.IsNullOrWhiteSpace(client.AllowedReturnUrlsJson))
        {
            return null;
        }

        try
        {
            var urls = JsonSerializer.Deserialize<List<string>>(client.AllowedReturnUrlsJson) ?? [];
            return urls.FirstOrDefault(u => !string.IsNullOrWhiteSpace(u));
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
