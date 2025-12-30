using Microsoft.AspNetCore.Components;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Application.Admin.Dtos;
using Radzen;
using Radzen.Blazor;

namespace Auth.Web.Components.Admin.Areas;

public partial class Areas : ComponentBase
{
    [Inject] private IAdminAreaService AdminAreaService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;

    private AreasViewModel _vm = null!;
    private RadzenDataGrid<AreaAdminDto> grid = null!;
    private readonly Dictionary<AreaAdminDto, string> _nameBuffer = new();
    private readonly List<AreaAdminDto> _areasToInsert = new();
    private readonly List<AreaAdminDto> _areasToUpdate = new();

    private List<AreaAdminDto> areas => _vm.Areas;
    private string editName
    {
        get => _vm.EditName;
        set => _vm.EditName = value;
    }

    private bool IsLoading { get; set; }
    private bool IsSaving { get; set; }

    protected override void OnInitialized()
    {
        _vm = new AreasViewModel(AdminAreaService);
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
            _areasToInsert.Clear();
            _areasToUpdate.Clear();
            
            if (reloadGrid && grid is not null)
            {
                await grid.Reload();
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "No se pudieron cargar las áreas.", ex.Message);
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private string GetNameBuffer(AreaAdminDto area)
    {
        if (!_nameBuffer.TryGetValue(area, out var value))
        {
            value = area.Name;
            _nameBuffer[area] = value;
        }
        return value;
    }

    private void SetNameBuffer(AreaAdminDto area, string value)
    {
        _nameBuffer[area] = value;
    }

    private async Task BeginCreate()
    {
        if (IsLoading || IsSaving)
        {
            return;
        }

        // Prevent multiple pending CREATE rows (single-create-at-a-time)
        if (_areasToInsert.Count > 0)
        {
            return;
        }

        _vm.BeginCreate();
        var newArea = new AreaAdminDto { Id = 0, Name = string.Empty, UserCount = 0 };
        _areasToInsert.Add(newArea);
        areas.Insert(0, newArea);
        await grid.InsertRow(newArea);
    }

    private async Task ValidateAndSave(AreaAdminDto area)
    {
        if (IsLoading || IsSaving)
        {
            return;
        }

        // Set VM context for validation: for EDIT set BeginEdit; for CREATE EditName is already set from buffer
        if (area.Id != 0)
        {
            _vm.BeginEdit(area);
        }
        else
        {
            // For CREATE: ensure EditName is synced from buffer for pre-validation
            _vm.EditName = GetNameBuffer(area);
        }

        var name = GetNameBuffer(area);
        var validationResult = _vm.ValidateOnly(name);

        if (validationResult.Outcome != AreasVmOutcome.Success)
        {
            NotifyUser(validationResult);
            // For CREATE: Do NOT call grid.UpdateRow - validation failed before persistence
            // Keep the row in edit mode by not exiting; grid is already in edit mode
            return;
        }

        await grid.UpdateRow(area);
    }

    private async Task OnRowCreate(AreaAdminDto area)
    {
        if (IsSaving)
        {
            return;
        }

        IsSaving = true;
        try
        {
            // EditName already set from ValidateAndSave; just call SaveAsync
            editName = GetNameBuffer(area);
            var result = await _vm.SaveAsync();
            NotifyUser(result);

            if (result.Outcome == AreasVmOutcome.Success)
            {
                // CRITICAL: Sync buffer ? DTO so grid displays the new name in view mode
                area.Name = editName.Trim();
                
                // Apply CreatedId from service (no reload needed)
                if (result.CreatedId.HasValue)
                {
                    area.Id = result.CreatedId.Value;
                }
                
                _areasToInsert.Remove(area);
                _nameBuffer.Remove(area);
                
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

    private async Task EditRow(AreaAdminDto area)
    {
        if (IsLoading || IsSaving)
        {
            return;
        }

        if (!_areasToUpdate.Contains(area))
        {
            _areasToUpdate.Add(area);
        }
        await grid.EditRow(area);
    }

    private async Task OnRowUpdate(AreaAdminDto area)
    {
        if (IsSaving)
        {
            return;
        }

        IsSaving = true;
        try
        {
            _vm.BeginEdit(area);
            editName = GetNameBuffer(area);
            var result = await _vm.SaveAsync();
            NotifyUser(result);

            if (result.Outcome == AreasVmOutcome.Success)
            {
                // CRITICAL: Sync buffer ? DTO so grid displays the updated name in view mode
                area.Name = editName.Trim();
                
                _areasToUpdate.Remove(area);
                _nameBuffer.Remove(area);
                
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

    private async Task DeleteArea(int id)
    {
        if (IsSaving)
        {
            return;
        }

        var confirm = await DialogService.Confirm("¿Eliminar el área?", "Confirmar", new ConfirmOptions { OkButtonText = "Eliminar", CancelButtonText = "Cancelar", Icon = "warning" });
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

    private void CancelEditRow(AreaAdminDto area)
    {
        if (IsSaving)
        {
            return;
        }

        grid.CancelEditRow(area);
        _nameBuffer.Remove(area);
        _areasToInsert.Remove(area);
        _areasToUpdate.Remove(area);
        
        if (area.Id == 0)
        {
            areas.Remove(area);
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

    private void NotifyUser(AreasVmResult result)
    {
        var severity = result.Outcome switch
        {
            AreasVmOutcome.Success => NotificationSeverity.Success,
            AreasVmOutcome.ValidationError => NotificationSeverity.Warning,
            AreasVmOutcome.Error => NotificationSeverity.Error,
            _ => NotificationSeverity.Info
        };

        NotificationService.Notify(severity, result.Title, result.Message);
    }
}
