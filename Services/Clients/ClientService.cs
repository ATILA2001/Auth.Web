using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Auth.Web.Data;
using Auth.Web.Domain.Entities;
using Auth.Web.Services.Abstractions; // legacy
using Microsoft.EntityFrameworkCore;
using AppClientService = Auth.Web.Application.Abstractions.IClientService;

namespace Auth.Web.Services.Clients;

public class ClientService : IClientService, AppClientService
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
