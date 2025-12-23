using Microsoft.AspNetCore.Components;
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

    private List<RoleAdminDto> roles => _vm.Roles;
    private RoleAdminDto editModel => _vm.EditModel;
    private bool editing => _vm.Editing;
    private string editName
    {
        get => _vm.EditName;
        set => _vm.EditName = value;
    }
    private string? validationError => _vm.ValidationError;

    protected override void OnInitialized()
    {
        _vm = new RolesViewModel(RoleService);
    }

    protected override async Task OnInitializedAsync()
    {
        await _vm.LoadAsync();
    }

    private void BeginCreate()
    {
        _vm.BeginCreate();
        roleForm = new RoleFormModel();
    }

    private void BeginEdit(RoleAdminDto dto)
    {
        _vm.BeginEdit(dto);
        roleForm = new RoleFormModel { Name = editName };
    }

    private async Task OnSubmitRole()
    {
        editName = roleForm.Name;
        var result = await _vm.SaveAsync();
        NotifyUser(result);

        if (result.RequiresReload)
        {
            await _vm.LoadAsync();
        }

        if (result.Outcome != RolesVmOutcome.ValidationError)
        {
            _vm.CancelEdit();
            roleForm = new RoleFormModel();
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

    private void CancelEdit()
    {
        _vm.CancelEdit();
        roleForm = new RoleFormModel();
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
