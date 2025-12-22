using Microsoft.AspNetCore.Components;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Application.Admin.Dtos;
using Radzen;
using Radzen.Blazor;

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

    private string? filterRoleId
    {
        get => _vm.FilterRoleId;
        set => _vm.FilterRoleId = value;
    }

    private int? filterPageId
    {
        get => _vm.FilterPageId;
        set => _vm.FilterPageId = value;
    }

    protected override void OnInitialized()
    {
        _vm = new PermissionsViewModel(PermissionService, RoleService, PageService, ActionService);
    }

    protected override async Task OnInitializedAsync()
    {
        await _vm.LoadAsync();
    }

    private void BeginCreate() => _vm.BeginCreate();

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

    private void CancelEdit() => _vm.CancelEdit();

    private async Task ApplyFilter()
    {
        await _vm.ApplyFilterAsync();
        await grid.Reload();
    }

    private async Task ClearFilter()
    {
        await _vm.ClearFilterAsync();
        await grid.Reload();
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
