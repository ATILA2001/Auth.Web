using Microsoft.AspNetCore.Identity;

namespace Auth.Web.Repositories.Abstractions.Admin;

public interface IRoleAdminRepository
{
    Task<List<IdentityRole>> GetRolesAsync(CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, int>> GetRoleUserCountsAsync(CancellationToken ct = default);
    Task<int> GetRoleUserCountAsync(string roleId, CancellationToken ct = default);
}
