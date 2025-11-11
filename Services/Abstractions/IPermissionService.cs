using Auth.Web.Domain.Dtos;

namespace Auth.Web.Services.Abstractions;

public interface IPermissionService
{
    Task<UserPermissionsDto> GetAsync(string userName);
}
