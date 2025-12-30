using Microsoft.AspNetCore.Components;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Application.Admin.Dtos;
using Radzen;
using Radzen.Blazor;

namespace Auth.Web.Components.Admin.Actions;

public partial class Actions : ComponentBase
{
    [Inject] private IAdminActionPermissionService AdminActionService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;

    private ActionsViewModel _vm = null!;
    private RadzenDataGrid<ActionPermissionAdminDto> grid = null!;
    private readonly Dictionary<ActionPermissionAdminDto, string> _nameBuffer = new();
    private readonly List<ActionPermissionAdminDto> _actionsToInsert = new();
    private readonly List<ActionPermissionAdminDto> _actionsToUpdate = new();

    private List<ActionPermissionAdminDto> actions => _vm.Actions;
    private string editName
    {
        get => _vm.EditName;
        set => _vm.EditName = value;
    }

    private bool IsLoading { get; set; }
    private bool IsSaving { get; set; }

    protected override void OnInitialized()
    {
        _vm = new ActionsViewModel(AdminActionService);
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
            _actionsToInsert.Clear();
            _actionsToUpdate.Clear();
            
            if (reloadGrid && grid is not null)
            {
                await grid.Reload();
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "No se pudieron cargar las acciones.", ex.Message);
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private string GetNameBuffer(ActionPermissionAdminDto action)
    {
        if (!_nameBuffer.TryGetValue(action, out var value))
        {
            value = action.Name;
            _nameBuffer[action] = value;
        }
        return value;
    }

    private void SetNameBuffer(ActionPermissionAdminDto action, string value)
    {
        _nameBuffer[action] = value;
    }

    private async Task BeginCreate()
    {
        if (IsLoading || IsSaving)
        {
            return;
        }

        // Prevent multiple pending CREATE rows (single-create-at-a-time)
        if (_actionsToInsert.Count > 0)
        {
            return;
        }

        _vm.BeginCreate();
        var newAction = new ActionPermissionAdminDto { Id = 0, Name = string.Empty, UsageCount = 0 };
        _actionsToInsert.Add(newAction);
        actions.Insert(0, newAction);
        await grid.InsertRow(newAction);
    }

    private async Task ValidateAndSave(ActionPermissionAdminDto action)
    {
        if (IsLoading || IsSaving)
        {
            return;
        }

        // Set VM context for validation: for EDIT set BeginEdit; for CREATE EditName is already set from buffer
        if (action.Id != 0)
        {
            _vm.BeginEdit(action);
        }
        else
        {
            // For CREATE: ensure EditName is synced from buffer for pre-validation
            _vm.EditName = GetNameBuffer(action);
        }

        var name = GetNameBuffer(action);
        var validationResult = _vm.ValidateOnly(name);

        if (validationResult.Outcome != ActionsVmOutcome.Success)
        {
            NotifyUser(validationResult);
            // For CREATE: Do NOT call grid.UpdateRow - validation failed before persistence
            // Keep the row in edit mode by not exiting; grid is already in edit mode
            return;
        }

        await grid.UpdateRow(action);
    }

    private async Task OnRowCreate(ActionPermissionAdminDto action)
    {
        if (IsSaving)
        {
            return;
        }

        IsSaving = true;
        try
        {
            // EditName already set from ValidateAndSave; just call SaveAsync
            editName = GetNameBuffer(action);
            var result = await _vm.SaveAsync();
            NotifyUser(result);

            if (result.Outcome == ActionsVmOutcome.Success)
            {
                // CRITICAL: Sync buffer ? DTO so grid displays the new name in view mode
                action.Name = editName.Trim();
                
                // Apply CreatedId from service (no reload needed)
                if (result.CreatedId.HasValue)
                {
                    action.Id = result.CreatedId.Value;
                }
                
                _actionsToInsert.Remove(action);
                _nameBuffer.Remove(action);
                
                // Only reload if CreatedId is missing (fallback)
                if (!result.CreatedId.HasValue && result.RequiresReload)
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

    private async Task EditRow(ActionPermissionAdminDto action)
    {
        if (IsLoading || IsSaving)
        {
            return;
        }

        if (!_actionsToUpdate.Contains(action))
        {
            _actionsToUpdate.Add(action);
        }
        await grid.EditRow(action);
    }

    private async Task OnRowUpdate(ActionPermissionAdminDto action)
    {
        if (IsSaving)
        {
            return;
        }

        IsSaving = true;
        try
        {
            _vm.BeginEdit(action);
            editName = GetNameBuffer(action);
            var result = await _vm.SaveAsync();
            NotifyUser(result);

            if (result.Outcome == ActionsVmOutcome.Success)
            {
                // CRITICAL: Sync buffer ? DTO so grid displays the updated name in view mode
                action.Name = editName.Trim();
                
                _actionsToUpdate.Remove(action);
                _nameBuffer.Remove(action);
                
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

    private async Task DeleteAction(int id)
    {
        if (IsSaving)
        {
            return;
        }

        var confirm = await DialogService.Confirm("Eliminar la acción?", "Confirmar", new ConfirmOptions { OkButtonText = "Eliminar", CancelButtonText = "Cancelar", Icon = "warning" });
        if (confirm != true)
        {
            return;
        }

        IsSaving = true;
        try
        {
            var result = await _vm.DeleteAsync(id);
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

    private void CancelEditRow(ActionPermissionAdminDto action)
    {
        if (IsSaving)
        {
            return;
        }

        grid.CancelEditRow(action);
        _nameBuffer.Remove(action);
        _actionsToInsert.Remove(action);
        _actionsToUpdate.Remove(action);
        
        if (action.Id == 0)
        {
            actions.Remove(action);
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

    private void NotifyUser(ActionsVmResult result)
    {
        var severity = result.Outcome switch
        {
            ActionsVmOutcome.Success => NotificationSeverity.Success,
            ActionsVmOutcome.ValidationError => NotificationSeverity.Warning,
            ActionsVmOutcome.Error => NotificationSeverity.Error,
            _ => NotificationSeverity.Info
        };

        NotificationService.Notify(severity, result.Title, result.Message);
    }
}
