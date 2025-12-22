using System.Text.Json;
using Auth.Web.Data;
using Auth.Web.Data.Entities;
using Auth.Web.Repositories.Abstractions.Admin;
using Microsoft.EntityFrameworkCore;

namespace Auth.Web.Repositories.Implementations.Admin;

public sealed class ClientAdminRepository : IClientAdminRepository
{
    private readonly IDbContextFactory<AuthDbContext> _dbFactory;

    public ClientAdminRepository(IDbContextFactory<AuthDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IReadOnlyCollection<ApplicationClient>> GetClientsAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.ApplicationClients.AsNoTracking().OrderBy(c => c.ClientId).ToListAsync(ct);
    }

    public async Task<ApplicationClient?> GetClientAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.ApplicationClients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<int> CreateClientAsync(string clientId, string audience, IEnumerable<string> allowedUrls, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        clientId = clientId.Trim();
        audience = audience.Trim();
        if (await db.ApplicationClients.AnyAsync(c => c.ClientId == clientId, ct))
            throw new InvalidOperationException("ClientId duplicado.");
        var list = NormalizeUrls(allowedUrls);
        var entity = new ApplicationClient
        {
            ClientId = clientId,
            Audience = audience,
            AllowedReturnUrlsJson = JsonSerializer.Serialize(list)
        };
        db.ApplicationClients.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task UpdateClientAsync(int id, string clientId, string audience, IEnumerable<string> allowedUrls, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var entity = await db.ApplicationClients.FindAsync(new object[] { id }, ct) ?? throw new KeyNotFoundException("Cliente no encontrado.");
        clientId = clientId.Trim();
        audience = audience.Trim();
        if (entity.ClientId != clientId && await db.ApplicationClients.AnyAsync(c => c.ClientId == clientId, ct))
            throw new InvalidOperationException("ClientId duplicado.");
        var list = NormalizeUrls(allowedUrls);
        entity.ClientId = clientId;
        entity.Audience = audience;
        entity.AllowedReturnUrlsJson = JsonSerializer.Serialize(list);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteClientAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var entity = await db.ApplicationClients.FindAsync(new object[] { id }, ct);
        if (entity is null) return;
        db.ApplicationClients.Remove(entity);
        await db.SaveChangesAsync(ct);
    }

    private static List<string> NormalizeUrls(IEnumerable<string> urls)
        => urls.Where(u => !string.IsNullOrWhiteSpace(u)).Select(u => u.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
}
