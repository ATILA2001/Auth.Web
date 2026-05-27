using Auth.Web.Application.Admin.Dtos;

namespace Auth.Web.Services.Abstractions.Admin;

public interface IAdminUserPageOverrideService
{
    Task<IReadOnlyCollection<UserPageOverrideAdminDto>> GetOverridesByUserAsync(string userId, CancellationToken ct = default);
    Task<int> CreateOverrideAsync(string userId, int? pageId, int? actionPermissionId, bool isAllowed, CancellationToken ct = default);
    Task DeleteOverrideAsync(int id, CancellationToken ct = default);
}
