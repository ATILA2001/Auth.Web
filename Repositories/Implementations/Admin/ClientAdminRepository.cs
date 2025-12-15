using System.Text.Json;
using Auth.Web.Data;
using Auth.Web.Data.Entities;
using Auth.Web.Repositories.Abstractions.Admin;
using Microsoft.EntityFrameworkCore;

namespace Auth.Web.Repositories.Implementations.Admin;

public sealed class ClientAdminRepository : IClientAdminRepository
{
    private readonly AuthDbContext _db;

    public ClientAdminRepository(AuthDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyCollection<ApplicationClient>> GetClientsAsync(CancellationToken ct = default)
        => await _db.ApplicationClients.AsNoTracking().OrderBy(c => c.ClientId).ToListAsync(ct);

    public Task<ApplicationClient?> GetClientAsync(int id, CancellationToken ct = default)
        => _db.ApplicationClients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<int> CreateClientAsync(string clientId, string audience, IEnumerable<string> allowedUrls, CancellationToken ct = default)
    {
        clientId = clientId.Trim();
        audience = audience.Trim();
        if (await _db.ApplicationClients.AnyAsync(c => c.ClientId == clientId, ct))
            throw new InvalidOperationException("ClientId duplicado.");
        var list = NormalizeUrls(allowedUrls);
        var entity = new ApplicationClient
        {
            ClientId = clientId,
            Audience = audience,
            AllowedReturnUrlsJson = JsonSerializer.Serialize(list)
        };
        _db.ApplicationClients.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task UpdateClientAsync(int id, string clientId, string audience, IEnumerable<string> allowedUrls, CancellationToken ct = default)
    {
        var entity = await _db.ApplicationClients.FindAsync(new object[] { id }, ct) ?? throw new KeyNotFoundException("Cliente no encontrado.");
        clientId = clientId.Trim();
        audience = audience.Trim();
        if (entity.ClientId != clientId && await _db.ApplicationClients.AnyAsync(c => c.ClientId == clientId, ct))
            throw new InvalidOperationException("ClientId duplicado.");
        var list = NormalizeUrls(allowedUrls);
        entity.ClientId = clientId;
        entity.Audience = audience;
        entity.AllowedReturnUrlsJson = JsonSerializer.Serialize(list);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteClientAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.ApplicationClients.FindAsync(new object[] { id }, ct);
        if (entity is null) return;
        _db.ApplicationClients.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    private static List<string> NormalizeUrls(IEnumerable<string> urls)
        => urls.Where(u => !string.IsNullOrWhiteSpace(u)).Select(u => u.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
}
