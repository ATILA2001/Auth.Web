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
            _nameBuffer.Clear();
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

        _vm.BeginCreate();
        var newArea = new AreaAdminDto { Id = 0, Name = string.Empty, UserCount = 0 };
        areas.Insert(0, newArea);
        await grid.InsertRow(newArea);
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
            _vm.BeginCreate();
            editName = GetNameBuffer(area);
            var result = await _vm.SaveAsync();
            NotifyUser(result);

            if (result.RequiresReload)
            {
                await LoadAsync(reloadGrid: true);
            }

            if (result.Outcome == AreasVmOutcome.ValidationError)
            {
                areas.Remove(area);
                await grid.Reload();
            }
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
