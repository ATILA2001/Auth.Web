using Auth.Web.Data;
using Auth.Web.Data.Entities;
using Auth.Web.Repositories.Abstractions.Clients;
using Microsoft.EntityFrameworkCore;

namespace Auth.Web.Repositories.Implementations.Clients;

public sealed class ClientRepository : IClientRepository
{
    private readonly AuthDbContext _db;

    public ClientRepository(AuthDbContext db)
    {
        _db = db;
    }

    public Task<ApplicationClient?> GetAsync(string clientId, CancellationToken ct = default)
    {
        return _db.ApplicationClients.SingleOrDefaultAsync(c => c.ClientId == clientId, ct);
    }
}
