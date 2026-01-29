using Auth.Web.Application.Auth;
using Auth.Web.Application.Permissions.Dtos;

namespace Auth.Web.Services.Abstractions.Permissions;

public interface IPermissionService
{
    Task<UserPermissionsDto> GetAsync(string userName, IReadOnlyCollection<string>? roleNamesOverride = null, IReadOnlyCollection<int>? areaIdsOverride = null);
}
