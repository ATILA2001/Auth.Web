using Microsoft.AspNetCore.Components;
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

    // Expose VM state for Razor binding
    private List<RolePagePermissionAdminDto> permissions => _vm.Permissions;
    private List<RoleAdminDto> roles => _vm.Roles;
    private List<PageAdminDto> pages => _vm.Pages;
    private List<ActionPermissionAdminDto> actions => _vm.Actions;
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
    }

    private async Task BeginCreate()
    {
        _vm.BeginCreate();

        var newPermission = new RolePagePermissionAdminDto
        {
            RoleId = selectedRoleId,
            RoleName = roles.FirstOrDefault(r => r.Id == selectedRoleId)?.Name ?? string.Empty,
            PageId = selectedPageId,
            PageName = pages.FirstOrDefault(p => p.Id == selectedPageId)?.Name ?? string.Empty,
            PageUrl = pages.FirstOrDefault(p => p.Id == selectedPageId)?.Url ?? string.Empty,
            ActionPermissionId = selectedActionId,
            ActionName = actions.FirstOrDefault(a => a.Id == selectedActionId)?.Name ?? string.Empty
        };

        permissions.Insert(0, newPermission);
        await grid.InsertRow(newPermission);
    }

    private async Task OnRowCreate(RolePagePermissionAdminDto permission)
    {
        _vm.SelectedRoleId = permission.RoleId;
        _vm.SelectedPageId = permission.PageId;
        _vm.SelectedActionId = permission.ActionPermissionId;

        var result = await _vm.SaveAsync();
        NotifyUser(result);

        if (result.RequiresReload)
        {
            await _vm.LoadPermissionsAsync();
            await grid.Reload();
        }

        if (result.Outcome == PermissionsVmOutcome.ValidationError)
        {
            permissions.Remove(permission);
            await grid.Reload();
        }
    }

    private async Task EditRow(RolePagePermissionAdminDto permission)
    {
        await grid.EditRow(permission);
    }

    private async Task OnRowUpdate(RolePagePermissionAdminDto permission)
    {
        // Si es edición de existente, eliminamos el permiso anterior antes de crear el nuevo
        if (permission.Id != 0)
        {
            var deleteResult = await _vm.DeleteAsync(permission.Id);
            if (deleteResult.Outcome == PermissionsVmOutcome.Error)
            {
                NotifyUser(deleteResult);
                await _vm.LoadPermissionsAsync();
                await grid.Reload();
                return;
            }
        }

        _vm.SelectedRoleId = permission.RoleId;
        _vm.SelectedPageId = permission.PageId;
        _vm.SelectedActionId = permission.ActionPermissionId;

        var result = await _vm.SaveAsync();
        NotifyUser(result);

        if (result.RequiresReload)
        {
            await _vm.LoadPermissionsAsync();
            await grid.Reload();
        }

        if (result.Outcome == PermissionsVmOutcome.ValidationError && permission.Id == 0)
        {
            permissions.Remove(permission);
            await grid.Reload();
        }
    }

    private void OnRoleChanged(RolePagePermissionAdminDto permission, string? roleId)
    {
        permission.RoleId = roleId ?? string.Empty;
        permission.RoleName = roles.FirstOrDefault(r => r.Id == permission.RoleId)?.Name ?? string.Empty;
    }

    private void OnPageChanged(RolePagePermissionAdminDto permission, int pageId)
    {
        permission.PageId = pageId;
        var page = pages.FirstOrDefault(p => p.Id == pageId);
        permission.PageName = page?.Name ?? string.Empty;
        permission.PageUrl = page?.Url ?? string.Empty;
    }

    private void OnActionChanged(RolePagePermissionAdminDto permission, int actionId)
    {
        permission.ActionPermissionId = actionId;
        permission.ActionName = actions.FirstOrDefault(a => a.Id == actionId)?.Name ?? string.Empty;
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

    private void CancelEditRow(RolePagePermissionAdminDto permission)
    {
        grid.CancelEditRow(permission);
        if (permission.Id == 0)
        {
            permissions.Remove(permission);
        }
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

    private void ClearFilters()
    {
        grid.Reset(true);
    }
}
