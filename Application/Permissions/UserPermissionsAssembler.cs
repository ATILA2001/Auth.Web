using Auth.Web.Application.Auth;
using Auth.Web.Application.Permissions.Dtos;
using Auth.Web.Data.Entities;
using System.Text.Json;

namespace Auth.Web.Application.Permissions;

public sealed class UserPermissionsAssembler
{
    // Intención: transformar datos crudos (roles, áreas, permisos DTO) en AuthClaimsModel.
    public AuthClaimsModel BuildClaims(ApplicationUser user, IReadOnlyCollection<string> roles, UserPermissionsDto rawPermissions, IReadOnlyCollection<string> apps)
    {
        // Areas: ahora usamos exclusivamente nombres. Si no hay nombres, devolvemos colección vacía.
        var areaCodes = (rawPermissions.AreaNames != null && rawPermissions.AreaNames.Count > 0)
            ? rawPermissions.AreaNames.ToArray()
            : Array.Empty<string>();

        // Serializar permisos por página/acción en JSON compacto (camelCase)
        var pages = rawPermissions.Pages
            .Select(p => new
            {
                url = p.Url,
                actions = p.Actions.Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
            })
            .ToArray();

        var permissionsPayload = new
        {
            pages,
            version = rawPermissions.Version
        };

        var permissionsJson = JsonSerializer.Serialize(permissionsPayload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        var displayName = user.Nombre;
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = user.UserName;
            if (!string.IsNullOrWhiteSpace(displayName) && displayName.Contains('@'))
            {
                displayName = displayName.Split('@')[0];
            }
        }

        return new AuthClaimsModel
        {
            UserId = user.Id,
            Email = user.Email,
            DisplayName = displayName,
            Roles = roles.ToArray(),
            Areas = areaCodes,
            Apps = apps.ToArray(),
            Pages = rawPermissions.Pages.ToArray(),
            PermissionsVersion = rawPermissions.Version,
            PermissionsJson = permissionsJson,
            FirstPageUrl = pages.FirstOrDefault()?.url
        };
    }
}
