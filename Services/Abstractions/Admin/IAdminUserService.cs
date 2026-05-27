using Auth.Web.Application.Admin.Dtos;

namespace Auth.Web.Services.Abstractions.Admin;

public interface IAdminUserService
{
    Task<IReadOnlyCollection<UserAdminDto>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<UserAdminDto?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);
    Task UpdateUserRolesAndAreasAsync(string userId, IEnumerable<string> roles, IEnumerable<int> areaIds, bool? isActive = null, CancellationToken cancellationToken = default);
    Task UpdateUserRolesAndClientAppsAsync(string userId, IEnumerable<string> roles, IEnumerable<string> clientIds, CancellationToken cancellationToken = default);
    Task DeleteUserAsync(string userId, CancellationToken cancellationToken = default);
}
