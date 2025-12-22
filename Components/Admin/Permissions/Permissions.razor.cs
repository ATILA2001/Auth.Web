using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Application.Admin.Dtos;
using Radzen;
using Radzen.Blazor;
using System.ComponentModel.DataAnnotations;

namespace Auth.Web.Components.Admin.Permissions;

public partial class Permissions : ComponentBase
{
    [Inject] private IAdminRolePagePermissionService PermissionService { get; set; } = null!;
    [Inject] private IAdminRoleService RoleService { get; set; } = null!;
    [Inject] private IAdminPageService PageService { get; set; } = null!;
    [Inject] private IAdminActionPermissionService ActionService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;

    private PermissionsViewModel _vm = null!;
    private RadzenDataGrid<RolePagePermissionAdminDto> grid = null!;
    private PermissionsFormModel permissionForm = new();

    // Expose VM state for Razor binding
    private List<RolePagePermissionAdminDto> permissions => _vm.Permissions;
    private List<RoleAdminDto> roles => _vm.Roles;
    private List<PageAdminDto> pages => _vm.Pages;
    private List<ActionPermissionAdminDto> actions => _vm.Actions;
    private bool editing => _vm.Editing;
    private string? validationError => _vm.ValidationError;

    private string selectedRoleId
    {
        get => _vm.SelectedRoleId;
        set => _vm.SelectedRoleId = value;
    }

    private int selectedPageId
    {
        get => _vm.SelectedPageId;
        set => _vm.SelectedPageId = value;
    }

    private int selectedActionId
    {
        get => _vm.SelectedActionId;
        set => _vm.SelectedActionId = value;
    }

    protected override void OnInitialized()
    {
        _vm = new PermissionsViewModel(PermissionService, RoleService, PageService, ActionService);
    }

    protected override async Task OnInitializedAsync()
    {
        await _vm.LoadAsync();
        SyncFormFromVm();
    }

    private void BeginCreate()
    {
        _vm.BeginCreate();
        SyncFormFromVm();
    }

    private async Task SavePermission()
    {
        var result = await _vm.SaveAsync();
        NotifyUser(result);

        if (result.RequiresReload)
        {
            await _vm.LoadPermissionsAsync();
            await grid.Reload();
        }
    }

    private async Task OnSubmitPermission()
    {
        _vm.SelectedRoleId = permissionForm.RoleId;
        _vm.SelectedPageId = permissionForm.PageId;
        _vm.SelectedActionId = permissionForm.ActionId;
        await SavePermission();
    }

    private async Task DeletePermission(int id)
    {
        var confirm = await DialogService.Confirm("¿Eliminar el permiso?", "Confirmar", new ConfirmOptions { OkButtonText = "Eliminar", CancelButtonText = "Cancelar", Icon = "warning" });
        if (confirm != true)
        {
            return;
        }

        var result = await _vm.DeleteAsync(id);
        NotifyUser(result);

        if (result.RequiresReload)
        {
            await _vm.LoadPermissionsAsync();
            await grid.Reload();
        }
    }

    private void CancelEdit()
    {
        _vm.CancelEdit();
    }

    private void SyncFormFromVm()
    {
        permissionForm = new PermissionsFormModel
        {
            RoleId = _vm.SelectedRoleId,
            PageId = _vm.SelectedPageId,
            ActionId = _vm.SelectedActionId
        };
    }

    private void NotifyUser(PermissionsVmResult result)
    {
        var severity = result.Outcome switch
        {
            PermissionsVmOutcome.Success => NotificationSeverity.Success,
            PermissionsVmOutcome.ValidationError => NotificationSeverity.Warning,
            PermissionsVmOutcome.Error => NotificationSeverity.Error,
            _ => NotificationSeverity.Info
        };

        NotificationService.Notify(severity, result.Title, result.Message);
    }
}

public sealed class PermissionsFormModel
{
    [Required(ErrorMessage = "Rol requerido")]
    public string RoleId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Página requerida")]
    [Range(1, int.MaxValue, ErrorMessage = "Página requerida")]
    public int PageId { get; set; }

    [Required(ErrorMessage = "Acción requerida")]
    [Range(1, int.MaxValue, ErrorMessage = "Acción requerida")]
    public int ActionId { get; set; }
}
