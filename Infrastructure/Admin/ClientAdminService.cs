using System.Text.Json;
using Auth.Web.Application.Admin.Abstractions;
using Auth.Web.Application.Admin.Dtos;
using Auth.Web.Application.Abstractions; // use IClientService abstraction
using Auth.Web.Data;
using Auth.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Auth.Web.Infrastructure.Admin;

public sealed class ClientAdminService : IAdminClientService
{
    private readonly AuthDbContext _db;
    private readonly IClientService _clientService; // reuse IsReturnUrlAllowed via abstraction (reserved for future validations)

    public ClientAdminService(AuthDbContext db, IClientService clientService)
    {
        _db = db;
        _clientService = clientService;
    }

    public async Task<IReadOnlyCollection<ApplicationClientAdminDto>> GetClientsAsync(CancellationToken cancellationToken = default)
    {
        var clients = await _db.ApplicationClients.AsNoTracking().OrderBy(c => c.ClientId).ToListAsync(cancellationToken);
        return clients.Select(Map).ToList();
    }

    public async Task<ApplicationClientAdminDto?> GetClientAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.ApplicationClients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<int> CreateClientAsync(string clientId, string audience, IEnumerable<string> allowedUrls, CancellationToken cancellationToken = default)
    {
        clientId = clientId.Trim();
        audience = audience.Trim();
        if (await _db.ApplicationClients.AnyAsync(c => c.ClientId == clientId, cancellationToken))
        {
            throw new InvalidOperationException("ClientId duplicado.");
        }
        var list = NormalizeUrls(allowedUrls);
        var entity = new ApplicationClient
        {
            ClientId = clientId,
            Audience = audience,
            AllowedReturnUrlsJson = JsonSerializer.Serialize(list)
        };
        _db.ApplicationClients.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    public async Task UpdateClientAsync(int id, string clientId, string audience, IEnumerable<string> allowedUrls, CancellationToken cancellationToken = default)
    {
        var entity = await _db.ApplicationClients.FindAsync(new object[] { id }, cancellationToken);
        if (entity is null) throw new KeyNotFoundException("Cliente no encontrado.");
        clientId = clientId.Trim();
        audience = audience.Trim();
        if (entity.ClientId != clientId && await _db.ApplicationClients.AnyAsync(c => c.ClientId == clientId, cancellationToken))
        {
            throw new InvalidOperationException("ClientId duplicado.");
        }
        var list = NormalizeUrls(allowedUrls);
        entity.ClientId = clientId;
        entity.Audience = audience;
        entity.AllowedReturnUrlsJson = JsonSerializer.Serialize(list);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteClientAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.ApplicationClients.FindAsync(new object[] { id }, cancellationToken);
        if (entity is null) return;
        _db.ApplicationClients.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static List<string> NormalizeUrls(IEnumerable<string> urls)
        => urls.Where(u => !string.IsNullOrWhiteSpace(u)).Select(u => u.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    private static ApplicationClientAdminDto Map(ApplicationClient c)
    {
        List<string> allowed;
        try
        {
            allowed = string.IsNullOrWhiteSpace(c.AllowedReturnUrlsJson)
                ? new List<string>()
                : (JsonSerializer.Deserialize<List<string>>(c.AllowedReturnUrlsJson) ?? new List<string>());
        }
        catch
        {
            allowed = new List<string>();
        }
        return new ApplicationClientAdminDto
        {
            Id = c.Id,
            ClientId = c.ClientId,
            Audience = c.Audience,
            AllowedReturnUrls = allowed
        };
    }
}
