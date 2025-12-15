using Auth.Web.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace Auth.Web.Repositories.Abstractions.Admin;

public interface IUserAdminRepository
{
    Task<IReadOnlyCollection<ApplicationUser>> GetUsersAsync(CancellationToken ct = default);
    Task<ApplicationUser?> GetUserByIdAsync(string userId, CancellationToken ct = default);
    Task<IReadOnlyCollection<IdentityUserRole<string>>> GetUserRolesAsync(IEnumerable<string> userIds, CancellationToken ct = default);
    Task<IReadOnlyCollection<IdentityRole>> GetRolesByIdsAsync(IEnumerable<string> roleIds, CancellationToken ct = default);
    Task<IReadOnlyCollection<UserArea>> GetUserAreasAsync(IEnumerable<string> userIds, CancellationToken ct = default);
    Task<IReadOnlyCollection<Area>> GetAreasByIdsAsync(IEnumerable<int> areaIds, CancellationToken ct = default);
    Task UpdateUserAreasAsync(string userId, IEnumerable<int> areaIds, CancellationToken ct = default);
}
