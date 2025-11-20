using Auth.Web.Application.Auth;
using Auth.Web.Domain.Dtos;

namespace Auth.Web.Application.Abstractions;

public interface IPermissionService
{
    // Retorna DTO crudo (páginas, acciones, áreas) del sistema de permisos.
    Task<UserPermissionsDto> GetAsync(string userName);
}
