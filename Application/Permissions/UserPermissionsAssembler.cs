using Auth.Web.Application.Auth;
using Auth.Web.Application.Permissions.Dtos;
using Auth.Web.Data.Entities;

namespace Auth.Web.Application.Permissions;

public sealed class UserPermissionsAssembler
{
    // Intención: transformar datos crudos (roles, áreas, permisos DTO) en AuthClaimsModel.
    public AuthClaimsModel BuildClaims(ApplicationUser user, IReadOnlyCollection<string> roles, UserPermissionsDto rawPermissions, IReadOnlyCollection<string> apps)
    {
        // Areas: convertir a string para claims (usar Id numérico como string)
        var areaCodes = rawPermissions.Areas.Select(a => a.ToString()).ToArray();

        return new AuthClaimsModel
        {
            UserId = user.Id,
            Email = user.Email,
            DisplayName = user.Nombre ?? user.UserName,
            Roles = roles.ToArray(),
            Areas = areaCodes,
            Apps = apps.ToArray(),
            PermissionsVersion = rawPermissions.Version
        };
    }
}
