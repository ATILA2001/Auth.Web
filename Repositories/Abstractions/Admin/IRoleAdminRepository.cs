using Auth.Web.Application.Admin.Dtos;
using Microsoft.AspNetCore.Identity;

namespace Auth.Web.Repositories.Abstractions.Admin;

public interface IRoleAdminRepository
{
    Task<List<IdentityRole>> GetRolesAsync(CancellationToken ct = default);
    Task<IReadOnlyCollection<RoleAdminDto>> GetRolesWithUserCountAsync(CancellationToken ct = default);
    Task<RoleAdminDto?> GetRoleByIdWithUserCountAsync(string roleId, CancellationToken ct = default);
}
