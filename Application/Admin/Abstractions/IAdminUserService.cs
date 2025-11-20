using Auth.Web.Application.Admin.Dtos;

namespace Auth.Web.Application.Admin.Abstractions;

public interface IAdminUserService
{
    Task<IReadOnlyCollection<UserAdminDto>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<UserAdminDto?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);
    Task UpdateUserRolesAsync(string userId, IEnumerable<string> roles, CancellationToken cancellationToken = default); // TODO: prefer combined method
    Task UpdateUserAreasAsync(string userId, IEnumerable<int> areaIds, CancellationToken cancellationToken = default); // TODO: prefer combined method
    Task UpdateUserRolesAndAreasAsync(string userId, IEnumerable<string> roles, IEnumerable<int> areaIds, CancellationToken cancellationToken = default);
}
