using Auth.Web.Application.Admin.Dtos;

namespace Auth.Web.Repositories.Abstractions.Admin;

public interface IClientAdminRepository
{
    Task<IReadOnlyCollection<ApplicationClientAdminDto>> GetClientsAsync(CancellationToken ct = default);
    Task<ApplicationClientAdminDto?> GetClientAsync(int id, CancellationToken ct = default);
    Task<int> CreateClientAsync(string clientId, string audience, IEnumerable<string> allowedUrls, CancellationToken ct = default);
    Task UpdateClientAsync(int id, string clientId, string audience, IEnumerable<string> allowedUrls, CancellationToken ct = default);
    Task DeleteClientAsync(int id, CancellationToken ct = default);
}
