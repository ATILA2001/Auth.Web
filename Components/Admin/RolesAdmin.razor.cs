using Microsoft.AspNetCore.Components;
using Auth.Web.Application.Admin.Abstractions;
using Auth.Web.Application.Admin.Dtos;
using Radzen;

namespace Auth.Web.Components.Admin;

public partial class RolesAdmin : ComponentBase
{
    [Inject] private IAdminRoleService RoleService { get; set; } = default!;
    [Inject] private NotificationService Notifications { get; set; } = default!;

    private List<RoleAdminDto> roles = new();
    private string newRole = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        roles = (await RoleService.GetRolesAsync()).ToList();
    }

    private async Task CreateRole()
    {
        if (string.IsNullOrWhiteSpace(newRole))
        {
            Notifications.Notify(NotificationSeverity.Warning, "Validación", "El nombre del rol no puede estar vacío.");
            return;
        }

        try
        {
            var id = await RoleService.CreateRoleAsync(newRole);
            if (!string.IsNullOrWhiteSpace(id))
            {
                Notifications.Notify(NotificationSeverity.Success, "Rol creado", $"Se creó '{newRole}'.");
                newRole = string.Empty;
                roles = (await RoleService.GetRolesAsync()).ToList();
            }
            else
            {
                Notifications.Notify(NotificationSeverity.Warning, "Sin cambios", "Nombre inválido o duplicado.");
            }
        }
        catch (Exception ex)
        {
            Notifications.Notify(NotificationSeverity.Error, "Error al crear rol", ex.Message);
        }
    }

    private async Task DeleteRole(string roleId)
    {
        try
        {
            await RoleService.DeleteRoleAsync(roleId);
            Notifications.Notify(NotificationSeverity.Success, "Rol eliminado", $"Id {roleId} eliminado.");
            roles = (await RoleService.GetRolesAsync()).ToList();
        }
        catch (Exception ex)
        {
            Notifications.Notify(NotificationSeverity.Error, "Error al eliminar rol", ex.Message);
        }
    }
}
