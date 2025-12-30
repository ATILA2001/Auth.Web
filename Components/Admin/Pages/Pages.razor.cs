using Microsoft.AspNetCore.Components;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Application.Admin.Dtos;
using Radzen;
using Radzen.Blazor;

namespace Auth.Web.Components.Admin.Pages;

public partial class Pages : ComponentBase
{
    [Inject] private IAdminPageService AdminPageService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;

    private PagesViewModel _vm = null!;
    private RadzenDataGrid<PageAdminDto> grid = null!;
    private readonly Dictionary<PageAdminDto, string> _nameBuffer = new();
    private readonly Dictionary<PageAdminDto, string> _urlBuffer = new();
    private readonly List<PageAdminDto> _pagesToInsert = new();
    private readonly List<PageAdminDto> _pagesToUpdate = new();

    private List<PageAdminDto> pages => _vm.Pages;
    private string editName
    {
        get => _vm.EditName;
        set => _vm.EditName = value;
    }
    private string editUrl
    {
        get => _vm.EditUrl;
        set => _vm.EditUrl = value;
    }

    private bool IsLoading { get; set; }
    private bool IsSaving { get; set; }

    protected override void OnInitialized()
    {
        _vm = new PagesViewModel(AdminPageService);
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
            _urlBuffer.Clear();
            _pagesToInsert.Clear();
            _pagesToUpdate.Clear();
            
            if (reloadGrid && grid is not null)
            {
                await grid.Reload();
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "No se pudieron cargar las Páginas.", ex.Message);
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private string GetNameBuffer(PageAdminDto page)
    {
        if (!_nameBuffer.TryGetValue(page, out var value))
        {
            value = page.Name;
            _nameBuffer[page] = value;
        }
        return value;
    }

    private void SetNameBuffer(PageAdminDto page, string value)
    {
        _nameBuffer[page] = value;
    }

    private string GetUrlBuffer(PageAdminDto page)
    {
        if (!_urlBuffer.TryGetValue(page, out var value))
        {
            value = page.Url;
            _urlBuffer[page] = value;
        }
        return value;
    }

    private void SetUrlBuffer(PageAdminDto page, string value)
    {
        _urlBuffer[page] = value;
    }

    private async Task BeginCreate()
    {
        if (IsLoading || IsSaving)
        {
            return;
        }

        // Prevent multiple pending CREATE rows (single-create-at-a-time)
        if (_pagesToInsert.Count > 0)
        {
            return;
        }

        _vm.BeginCreate();
        var newPage = new PageAdminDto { Id = 0, Name = string.Empty, Url = string.Empty, PermissionCount = 0 };
        _pagesToInsert.Add(newPage);
        pages.Insert(0, newPage);
        await grid.InsertRow(newPage);
    }

    private async Task ValidateAndSave(PageAdminDto page)
    {
        if (IsLoading || IsSaving)
        {
            return;
        }

        // Set VM context for validation: for EDIT set BeginEdit; for CREATE EditFields already set from buffer
        if (page.Id != 0)
        {
            _vm.BeginEdit(page);
        }
        else
        {
            // For CREATE: ensure EditFields are synced from buffer for pre-validation
            _vm.EditName = GetNameBuffer(page);
            _vm.EditUrl = GetUrlBuffer(page);
        }

        var name = GetNameBuffer(page);
        var url = GetUrlBuffer(page);
        var validationResult = _vm.ValidateOnly(name, url);

        if (validationResult.Outcome != PagesVmOutcome.Success)
        {
            NotifyUser(validationResult);
            // For CREATE: Do NOT call grid.UpdateRow - validation failed before persistence
            // Keep the row in edit mode by not exiting; grid is already in edit mode
            return;
        }

        await grid.UpdateRow(page);
    }

    private async Task OnRowCreate(PageAdminDto page)
    {
        if (IsSaving)
        {
            return;
        }

        IsSaving = true;
        try
        {
            // EditFields already set from ValidateAndSave; just call SaveAsync
            editName = GetNameBuffer(page);
            editUrl = GetUrlBuffer(page);
            var result = await _vm.SaveAsync();
            NotifyUser(result);

            if (result.Outcome == PagesVmOutcome.Success)
            {
                // CRITICAL: Sync buffer ? DTO so grid displays the new values in view mode
                page.Name = editName.Trim();
                page.Url = editUrl.Trim();
                
                // Apply CreatedId from service (no reload needed)
                if (result.CreatedId.HasValue)
                {
                    page.Id = result.CreatedId.Value;
                }
                
                _pagesToInsert.Remove(page);
                _nameBuffer.Remove(page);
                _urlBuffer.Remove(page);
                
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

    private async Task EditRow(PageAdminDto page)
    {
        if (IsLoading || IsSaving)
        {
            return;
        }

        if (!_pagesToUpdate.Contains(page))
        {
            _pagesToUpdate.Add(page);
        }
        await grid.EditRow(page);
    }

    private async Task OnRowUpdate(PageAdminDto page)
    {
        if (IsSaving)
        {
            return;
        }

        IsSaving = true;
        try
        {
            _vm.BeginEdit(page);
            editName = GetNameBuffer(page);
            editUrl = GetUrlBuffer(page);
            var result = await _vm.SaveAsync();
            NotifyUser(result);

            if (result.Outcome == PagesVmOutcome.Success)
            {
                // CRITICAL: Sync buffer ? DTO so grid displays the updated values in view mode
                page.Name = editName.Trim();
                page.Url = editUrl.Trim();
                
                _pagesToUpdate.Remove(page);
                _nameBuffer.Remove(page);
                _urlBuffer.Remove(page);
                
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

    private async Task DeletePage(int id)
    {
        if (IsSaving)
        {
            return;
        }

        var confirm = await DialogService.Confirm("Eliminar la página?", "Confirmar", new ConfirmOptions { OkButtonText = "Eliminar", CancelButtonText = "Cancelar", Icon = "warning" });
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

    private void CancelEditRow(PageAdminDto page)
    {
        if (IsSaving)
        {
            return;
        }

        grid.CancelEditRow(page);
        _nameBuffer.Remove(page);
        _urlBuffer.Remove(page);
        _pagesToInsert.Remove(page);
        _pagesToUpdate.Remove(page);
        
        if (page.Id == 0)
        {
            pages.Remove(page);
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

    private void NotifyUser(PagesVmResult result)
    {
        var severity = result.Outcome switch
        {
            PagesVmOutcome.Success => NotificationSeverity.Success,
            PagesVmOutcome.ValidationError => NotificationSeverity.Warning,
            PagesVmOutcome.Error => NotificationSeverity.Error,
            _ => NotificationSeverity.Info
        };

        NotificationService.Notify(severity, result.Title, result.Message);
    }
}
