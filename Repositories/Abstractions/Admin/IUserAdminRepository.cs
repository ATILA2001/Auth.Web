using Auth.Web.Application.Admin.Dtos;

namespace Auth.Web.Repositories.Abstractions.Admin;

public interface IUserAdminRepository
{
    Task<IReadOnlyCollection<UserAdminDto>> GetUsersAsync(CancellationToken ct = default);
    Task<UserAdminDto?> GetUserByIdAsync(string userId, CancellationToken ct = default);
    Task UpdateUserAreasAsync(string userId, IEnumerable<int> areaIds, CancellationToken ct = default);
}
