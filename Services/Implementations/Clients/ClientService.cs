using System.Text.Json;
using Auth.Web.Data;
using Auth.Web.Domain.Entities;
using Auth.Web.Services.Abstractions.Clients;
using Microsoft.EntityFrameworkCore;

namespace Auth.Web.Services.Implementations.Clients;

public class ClientService : IClientService
{
    private readonly AuthDbContext _context;

    public ClientService(AuthDbContext context)
    {
        _context = context;
    }

    public Task<ApplicationClient?> GetAsync(string clientId)
    {
        return _context.ApplicationClients.SingleOrDefaultAsync(c => c.ClientId == clientId);
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
}
