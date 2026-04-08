using Auth.Web.Application.Permissions.Dtos;

namespace Auth.Web.Services.Abstractions.Permissions;

public interface IPermissionService
{
    Task<UserPermissionsDto> GetAsync(
        string userName,
        int? clientId = null,
        IReadOnlyCollection<string>? roleNamesOverride = null,
        IReadOnlyCollection<int>? areaIdsOverride = null);

    Task<int> GetVersionAsync(string userName);
    Task<int> GetVersionByUserIdAsync(string userId);
}
