using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Application.Admin.Dtos;
using Radzen;
using System.ComponentModel.DataAnnotations;

namespace Auth.Web.Components.Admin.Roles;

public partial class Roles : ComponentBase
{
    [Inject] private IAdminRoleService RoleService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;

    private RolesViewModel _vm = null!;
    private RoleFormModel roleForm = new();

    // Expose VM state with same names for Razor binding compatibility
    private List<RoleAdminDto> roles => _vm.Roles;
    private string newRole
    {
        get => _vm.NewRoleName;
        set => _vm.NewRoleName = value;
    }

    protected override void OnInitialized()
    {
        _vm = new RolesViewModel(RoleService);
    }

    protected override async Task OnInitializedAsync()
    {
        await _vm.LoadAsync();
    }

    private async Task OnSubmitRole()
    {
        newRole = roleForm.Name;
        await CreateRole();
        roleForm = new RoleFormModel();
    }

    private async Task CreateRole()
    {
        var result = await _vm.CreateAsync();
        NotifyUser(result);

        if (result.RequiresReload)
        {
            await _vm.LoadAsync();
        }
    }

    private async Task DeleteRole(string roleId)
    {
        var confirm = await DialogService.Confirm("¿Eliminar el rol?", "Confirmar", new ConfirmOptions { OkButtonText = "Eliminar", CancelButtonText = "Cancelar", Icon = "warning" });
        if (confirm != true)
        {
            return;
        }

        var result = await _vm.DeleteAsync(roleId);
        NotifyUser(result);

        if (result.RequiresReload)
        {
            await _vm.LoadAsync();
        }
    }

    private void NotifyUser(RolesVmResult result)
    {
        var severity = result.Outcome switch
        {
            RolesVmOutcome.Success => NotificationSeverity.Success,
            RolesVmOutcome.ValidationError => NotificationSeverity.Warning,
            RolesVmOutcome.Error => NotificationSeverity.Error,
            _ => NotificationSeverity.Info
        };

        NotificationService.Notify(severity, result.Title, result.Message);
    }
}

public sealed class RoleFormModel
{
    [Required(ErrorMessage = "Nombre requerido")]
    public string Name { get; set; } = string.Empty;
}
