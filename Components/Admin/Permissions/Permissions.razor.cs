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
    private readonly Dictionary<RolePagePermissionAdminDto, string> _roleBuffer = new();
    private readonly Dictionary<RolePagePermissionAdminDto, int?> _pageBuffer = new();
    private readonly Dictionary<RolePagePermissionAdminDto, int?> _actionBuffer = new();
    private readonly List<RolePagePermissionAdminDto> _permissionsToInsert = new();
    private readonly List<RolePagePermissionAdminDto> _permissionsToUpdate = new();

    private List<RolePagePermissionAdminDto> permissions => _vm.Permissions;
    private List<RoleAdminDto> roles => _vm.Roles;
    private List<PageAdminDto> pages => _vm.Pages;
    private List<ActionPermissionAdminDto> actions => _vm.Actions;

    private bool IsLoading { get; set; }
    private bool IsSaving { get; set; }

    protected override void OnInitialized()
    {
        _vm = new PermissionsViewModel(PermissionService, RoleService, PageService, ActionService);
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync(reloadGrid: false);
    }

    private async Task LoadAsync(bool reloadGrid)
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        try
        {
            await _vm.LoadAsync();
            
            // Clear tracking lists after reload to avoid stale references
            _roleBuffer.Clear();
            _pageBuffer.Clear();
            _actionBuffer.Clear();
            _permissionsToInsert.Clear();
            _permissionsToUpdate.Clear();
            
            if (reloadGrid && grid is not null)
            {
                await grid.Reload();
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "No se pudieron cargar los permisos.", ex.Message);
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private string GetRoleBuffer(RolePagePermissionAdminDto permission)
    {
        if (!_roleBuffer.TryGetValue(permission, out var value))
        {
            value = permission.RoleId;
            _roleBuffer[permission] = value;
        }
        return value;
    }

    private void SetRoleBuffer(RolePagePermissionAdminDto permission, string value)
    {
        _roleBuffer[permission] = value;
        permission.RoleName = roles.FirstOrDefault(r => r.Id == value)?.Name ?? string.Empty;
    }

    private int? GetPageBuffer(RolePagePermissionAdminDto permission)
    {
        if (!_pageBuffer.TryGetValue(permission, out var value))
        {
            value = permission.PageId;
            _pageBuffer[permission] = value;
        }
        return value;
    }

    private void SetPageBuffer(RolePagePermissionAdminDto permission, int? value)
    {
        _pageBuffer[permission] = value;
        if (!value.HasValue)
        {
            permission.PageName = "Sin asignar";
            permission.PageUrl = string.Empty;
            return;
        }
        var page = pages.FirstOrDefault(p => p.Id == value.Value);
        permission.PageName = page?.Name ?? "Sin asignar";
        permission.PageUrl = page?.Url ?? string.Empty;
    }

    private int? GetActionBuffer(RolePagePermissionAdminDto permission)
    {
        if (!_actionBuffer.TryGetValue(permission, out var value))
        {
            value = permission.ActionPermissionId;
            _actionBuffer[permission] = value;
        }
        return value;
    }

    private void SetActionBuffer(RolePagePermissionAdminDto permission, int? value)
    {
        _actionBuffer[permission] = value;
        if (!value.HasValue)
        {
            permission.ActionName = "Sin asignar";
            return;
        }
        permission.ActionName = actions.FirstOrDefault(a => a.Id == value.Value)?.Name ?? "Sin asignar";
    }

    private async Task BeginCreate()
    {
        if (IsLoading || IsSaving)
        {
            return;
        }

        // Prevent multiple pending CREATE rows (single-create-at-a-time)
        if (_permissionsToInsert.Count > 0)
        {
            return;
        }

        _vm.BeginCreate();
        var newPermission = new RolePagePermissionAdminDto
        {
            Id = 0,
            RoleId = _vm.SelectedRoleId,
            RoleName = roles.FirstOrDefault(r => r.Id == _vm.SelectedRoleId)?.Name ?? string.Empty,
            PageId = _vm.SelectedPageId,
            PageName = _vm.SelectedPageId.HasValue
                ? pages.FirstOrDefault(p => p.Id == _vm.SelectedPageId.Value)?.Name ?? "Sin asignar"
                : "Sin asignar",
            PageUrl = _vm.SelectedPageId.HasValue
                ? pages.FirstOrDefault(p => p.Id == _vm.SelectedPageId.Value)?.Url ?? string.Empty
                : string.Empty,
            ActionPermissionId = _vm.SelectedActionId,
            ActionName = _vm.SelectedActionId.HasValue
                ? actions.FirstOrDefault(a => a.Id == _vm.SelectedActionId.Value)?.Name ?? "Sin asignar"
                : "Sin asignar"
        };
        _permissionsToInsert.Add(newPermission);
        permissions.Insert(0, newPermission);
        await grid.InsertRow(newPermission);
    }

    private async Task ValidateAndSave(RolePagePermissionAdminDto permission)
    {
        if (IsLoading || IsSaving)
        {
            return;
        }

        // Set VM context for validation: for EDIT set BeginEdit; for CREATE sync from buffer
        if (permission.Id != 0)
        {
            _vm.BeginEdit(permission);
        }
        else
        {
            // For CREATE: ensure fields are synced from buffer for pre-validation
            _vm.SelectedRoleId = GetRoleBuffer(permission);
            _vm.SelectedPageId = GetPageBuffer(permission);
            _vm.SelectedActionId = GetActionBuffer(permission);
        }

        var roleId = GetRoleBuffer(permission);
        var pageId = GetPageBuffer(permission);
        var actionId = GetActionBuffer(permission);
        var validationResult = _vm.ValidateOnly(roleId, pageId, actionId, permission.Id);

        if (validationResult.Outcome != PermissionsVmOutcome.Success)
        {
            NotifyUser(validationResult);
            return;
        }

        await grid.UpdateRow(permission);
    }

    private async Task OnRowCreate(RolePagePermissionAdminDto permission)
    {
        if (IsSaving)
        {
            return;
        }

        IsSaving = true;
        try
        {
            _vm.SelectedRoleId = GetRoleBuffer(permission);
            _vm.SelectedPageId = GetPageBuffer(permission);
            _vm.SelectedActionId = GetActionBuffer(permission);

            var result = await _vm.SaveAsync();
            NotifyUser(result);

            if (result.Outcome == PermissionsVmOutcome.Success)
            {
                // CRITICAL: Sync buffers ? DTO
                permission.RoleId = _vm.SelectedRoleId;
                permission.PageId = _vm.SelectedPageId;
                permission.ActionPermissionId = _vm.SelectedActionId;
                permission.RoleName = roles.FirstOrDefault(r => r.Id == _vm.SelectedRoleId)?.Name ?? string.Empty;
                permission.PageName = _vm.SelectedPageId.HasValue
                    ? pages.FirstOrDefault(p => p.Id == _vm.SelectedPageId.Value)?.Name ?? "Sin asignar"
                    : "Sin asignar";
                permission.PageUrl = _vm.SelectedPageId.HasValue
                    ? pages.FirstOrDefault(p => p.Id == _vm.SelectedPageId.Value)?.Url ?? string.Empty
                    : string.Empty;
                permission.ActionName = _vm.SelectedActionId.HasValue
                    ? actions.FirstOrDefault(a => a.Id == _vm.SelectedActionId.Value)?.Name ?? "Sin asignar"
                    : "Sin asignar";

                if (result.CreatedId.HasValue)
                {
                    permission.Id = result.CreatedId.Value;
                }

                _permissionsToInsert.Remove(permission);
                _roleBuffer.Remove(permission);
                _pageBuffer.Remove(permission);
                _actionBuffer.Remove(permission);

                if (!result.CreatedId.HasValue && result.RequiresReload)
                {
                    await LoadAsync(reloadGrid: true);
                }
            }
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task EditRow(RolePagePermissionAdminDto permission)
    {
        if (IsLoading || IsSaving)
        {
            return;
        }

        if (!_permissionsToUpdate.Contains(permission))
        {
            _permissionsToUpdate.Add(permission);
        }
        await grid.EditRow(permission);
    }

    private async Task OnRowUpdate(RolePagePermissionAdminDto permission)
    {
        if (IsSaving)
        {
            return;
        }

        IsSaving = true;
        try
        {
            _vm.BeginEdit(permission);
            _vm.SelectedRoleId = GetRoleBuffer(permission);
            _vm.SelectedPageId = GetPageBuffer(permission);
            _vm.SelectedActionId = GetActionBuffer(permission);

            var result = await _vm.UpdateAsync();
            NotifyUser(result);

            if (result.Outcome == PermissionsVmOutcome.Success)
            {
                // CRITICAL: Sync buffers ? DTO so grid displays the updated values in view mode
                // Note: ID remains the same now (direct update, not delete+create)
                permission.RoleId = _vm.SelectedRoleId;
                permission.PageId = _vm.SelectedPageId;
                permission.ActionPermissionId = _vm.SelectedActionId;
                permission.RoleName = roles.FirstOrDefault(r => r.Id == _vm.SelectedRoleId)?.Name ?? string.Empty;
                permission.PageName = _vm.SelectedPageId.HasValue
                    ? pages.FirstOrDefault(p => p.Id == _vm.SelectedPageId.Value)?.Name ?? "Sin asignar"
                    : "Sin asignar";
                permission.PageUrl = _vm.SelectedPageId.HasValue
                    ? pages.FirstOrDefault(p => p.Id == _vm.SelectedPageId.Value)?.Url ?? string.Empty
                    : string.Empty;
                permission.ActionName = _vm.SelectedActionId.HasValue
                    ? actions.FirstOrDefault(a => a.Id == _vm.SelectedActionId.Value)?.Name ?? "Sin asignar"
                    : "Sin asignar";

                _permissionsToUpdate.Remove(permission);
                _roleBuffer.Remove(permission);
                _pageBuffer.Remove(permission);
                _actionBuffer.Remove(permission);

                // Explicit contract: UPDATE success does NOT require reload (RequiresReload=false)
                // Filters/pagination/sorting preserved via local update
                await InvokeAsync(StateHasChanged);
            }
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task DeletePermission(int id)
    {
        if (IsSaving)
        {
            return;
        }

        var confirm = await DialogService.Confirm("Eliminar el permiso?", "Confirmar", new ConfirmOptions { OkButtonText = "Eliminar", CancelButtonText = "Cancelar", Icon = "warning" });
        if (confirm != true)
        {
            return;
        }

        IsSaving = true;
        try
        {
            var result = await _vm.DeleteAsync(id);
            NotifyUser(result);

            if (result.RequiresReload)
            {
                await LoadAsync(reloadGrid: true);
            }
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void CancelEditRow(RolePagePermissionAdminDto permission)
    {
        if (IsSaving)
        {
            return;
        }

        grid.CancelEditRow(permission);
        _roleBuffer.Remove(permission);
        _pageBuffer.Remove(permission);
        _actionBuffer.Remove(permission);
        _permissionsToInsert.Remove(permission);
        _permissionsToUpdate.Remove(permission);
        
        if (permission.Id == 0)
        {
            permissions.Remove(permission);
        }
    }

    private void ClearFilters()
    {
        if (IsLoading || IsSaving)
        {
            return;
        }

        grid.Reset(true);
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
