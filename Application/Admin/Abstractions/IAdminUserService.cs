using Auth.Web.Application.Admin.Dtos;

namespace Auth.Web.Application.Admin.Abstractions;

public interface IAdminUserService
{
    Task<IReadOnlyCollection<UserAdminDto>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<UserAdminDto?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);
    Task UpdateUserRolesAndAreasAsync(string userId, IEnumerable<string> roles, IEnumerable<int> areaIds, CancellationToken cancellationToken = default);
}
