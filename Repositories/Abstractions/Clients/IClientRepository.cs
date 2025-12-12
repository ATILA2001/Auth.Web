using Auth.Web.Data.Entities;

namespace Auth.Web.Repositories.Abstractions.Clients;

public interface IClientRepository
{
    Task<ApplicationClient?> GetAsync(string clientId, CancellationToken ct = default);
}