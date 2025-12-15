using Auth.Web.Data.Entities;

namespace Auth.Web.Repositories.Abstractions.Admin;

public interface IClientAdminRepository
{
    Task<IReadOnlyCollection<ApplicationClient>> GetClientsAsync(CancellationToken ct = default);
    Task<ApplicationClient?> GetClientAsync(int id, CancellationToken ct = default);
    Task<int> CreateClientAsync(string clientId, string audience, IEnumerable<string> allowedUrls, CancellationToken ct = default);
    Task UpdateClientAsync(int id, string clientId, string audience, IEnumerable<string> allowedUrls, CancellationToken ct = default);
    Task DeleteClientAsync(int id, CancellationToken ct = default);
}
