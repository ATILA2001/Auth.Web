using Auth.Web.Application.Admin.Dtos;

namespace Auth.Web.Application.Admin.Abstractions;

public interface IAdminClientService
{
    Task<IReadOnlyCollection<ApplicationClientAdminDto>> GetClientsAsync(CancellationToken cancellationToken = default);
    Task<ApplicationClientAdminDto?> GetClientAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateClientAsync(string clientId, string audience, IEnumerable<string> allowedUrls, CancellationToken cancellationToken = default);
    Task UpdateClientAsync(int id, string clientId, string audience, IEnumerable<string> allowedUrls, CancellationToken cancellationToken = default);
    Task DeleteClientAsync(int id, CancellationToken cancellationToken = default);
}
