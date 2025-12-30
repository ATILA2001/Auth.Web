using Microsoft.AspNetCore.Components;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Application.Admin.Dtos;
using Radzen;
using Radzen.Blazor;

namespace Auth.Web.Components.Admin.Roles;

public partial class Roles : ComponentBase
{
    [Inject] private IAdminRoleService RoleService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;

    private RolesViewModel _vm = null!;
    private RadzenDataGrid<RoleAdminDto> grid = null!;
    private readonly Dictionary<RoleAdminDto, string> _nameBuffer = new();
    private readonly List<RoleAdminDto> _rolesToInsert = new();
    private readonly List<RoleAdminDto> _rolesToUpdate = new();

    private List<RoleAdminDto> roles => _vm.Roles;
    private string editName
    {
        get => _vm.EditName;
        set => _vm.EditName = value;
    }

    private bool IsLoading { get; set; }
    private bool IsSaving { get; set; }

    protected override void OnInitialized()
    {
        _vm = new RolesViewModel(RoleService);
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
            _nameBuffer.Clear();
            _rolesToInsert.Clear();
            _rolesToUpdate.Clear();
            
            if (reloadGrid && grid is not null)
            {
                await grid.Reload();
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "No se pudieron cargar los roles.", ex.Message);
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private string GetNameBuffer(RoleAdminDto role)
    {
        if (!_nameBuffer.TryGetValue(role, out var value))
        {
            value = role.Name;
            _nameBuffer[role] = value;
        }
        return value;
    }

    private void SetNameBuffer(RoleAdminDto role, string value)
    {
        _nameBuffer[role] = value;
    }

    private async Task BeginCreate()
    {
        if (IsLoading || IsSaving)
        {
            return;
        }

        // Prevent multiple pending CREATE rows (single-create-at-a-time)
        if (_rolesToInsert.Count > 0)
        {
            return;
        }

        _vm.BeginCreate();
        var newRole = new RoleAdminDto { Id = string.Empty, Name = string.Empty, UserCount = 0 };
        _rolesToInsert.Add(newRole);
        roles.Insert(0, newRole);
        await grid.InsertRow(newRole);
    }

    private async Task ValidateAndSave(RoleAdminDto role)
    {
        if (IsLoading || IsSaving)
        {
            return;
        }

        // Set VM context for validation: for EDIT set BeginEdit; for CREATE EditName is already set from buffer
        if (!string.IsNullOrWhiteSpace(role.Id))
        {
            _vm.BeginEdit(role);
        }
        else
        {
            // For CREATE: ensure EditName is synced from buffer for pre-validation
            _vm.EditName = GetNameBuffer(role);
        }

        var name = GetNameBuffer(role);
        var validationResult = _vm.ValidateOnly(name);

        if (validationResult.Outcome != RolesVmOutcome.Success)
        {
            NotifyUser(validationResult);
            // For CREATE: Do NOT call grid.UpdateRow - validation failed before persistence
            // Keep the row in edit mode by not exiting; grid is already in edit mode
            return;
        }

        await grid.UpdateRow(role);
    }

    private async Task OnRowCreate(RoleAdminDto role)
    {
        if (IsSaving)
        {
            return;
        }

        IsSaving = true;
        try
        {
            // EditName already set from ValidateAndSave; just call SaveAsync
            editName = GetNameBuffer(role);
            var result = await _vm.SaveAsync();
            NotifyUser(result);

            if (result.Outcome == RolesVmOutcome.Success)
            {
                // CRITICAL: Sync buffer ? DTO so grid displays the new name in view mode
                role.Name = editName.Trim();
                
                // Apply CreatedId from service (no reload needed)
                if (!string.IsNullOrEmpty(result.CreatedId))
                {
                    role.Id = result.CreatedId;
                }
                
                _rolesToInsert.Remove(role);
                _nameBuffer.Remove(role);
                
                // Only reload if CreatedId is missing (fallback)
                if (string.IsNullOrEmpty(result.CreatedId) && result.RequiresReload)
                {
                    await LoadAsync(reloadGrid: true);
                }
            }
            // Note: ValidationError case removed - pre-validation in ValidateAndSave prevents reaching this point
            // If SaveAsync returns ValidationError here, it's a service-layer issue, not UI validation
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task EditRow(RoleAdminDto role)
    {
        if (IsLoading || IsSaving)
        {
            return;
        }

        if (!_rolesToUpdate.Contains(role))
        {
            _rolesToUpdate.Add(role);
        }
        await grid.EditRow(role);
    }

    private async Task OnRowUpdate(RoleAdminDto role)
    {
        if (IsSaving)
        {
            return;
        }

        IsSaving = true;
        try
        {
            _vm.BeginEdit(role);
            editName = GetNameBuffer(role);
            var result = await _vm.SaveAsync();
            NotifyUser(result);

            if (result.Outcome == RolesVmOutcome.Success)
            {
                // CRITICAL: Sync buffer ? DTO so grid displays the updated name in view mode
                role.Name = editName.Trim();
                
                _rolesToUpdate.Remove(role);
                _nameBuffer.Remove(role);
                
                // Explicit contract: UPDATE success does NOT require reload (RequiresReload=false)
                // Filters/pagination/sorting preserved via local update
                await InvokeAsync(StateHasChanged);
            }
            // Note: ValidationError case removed - pre-validation in ValidateAndSave prevents reaching this point
            // If SaveAsync returns ValidationError here, it's a service-layer issue, not UI validation
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task DeleteRole(string roleId)
    {
        if (IsSaving)
        {
            return;
        }

        var confirm = await DialogService.Confirm("¿Eliminar el rol?", "Confirmar", new ConfirmOptions { OkButtonText = "Eliminar", CancelButtonText = "Cancelar", Icon = "warning" });
        if (confirm != true)
        {
            return;
        }

        IsSaving = true;
        try
        {
            var result = await _vm.DeleteAsync(roleId);
            NotifyUser(result);

            // Explicit contract: DELETE success requires reload to remove row from grid
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

    private void CancelEditRow(RoleAdminDto role)
    {
        if (IsSaving)
        {
            return;
        }

        grid.CancelEditRow(role);
        _nameBuffer.Remove(role);
        _rolesToInsert.Remove(role);
        _rolesToUpdate.Remove(role);
        
        if (string.IsNullOrWhiteSpace(role.Id))
        {
            roles.Remove(role);
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
