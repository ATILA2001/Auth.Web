using Auth.Web.Application.Auth;
using Auth.Web.Domain.Dtos;

namespace Auth.Web.Services.Abstractions.Permissions;

public interface IPermissionService
{
    Task<UserPermissionsDto> GetAsync(string userName);
}
