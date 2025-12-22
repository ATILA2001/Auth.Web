using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Application.Admin.Dtos;
using Radzen;
using Radzen.Blazor;
using System.ComponentModel.DataAnnotations;

namespace Auth.Web.Components.Admin.Areas;

public partial class Areas : ComponentBase
{
    [Inject] private IAdminAreaService AdminAreaService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;

    private AreasViewModel _vm = null!;
    private RadzenDataGrid<AreaAdminDto> grid = null!;
    private AreaFormModel areaForm = new();

    // Expose VM state with same names for Razor binding compatibility
    private List<AreaAdminDto> areas => _vm.Areas;
    private string newArea
    {
        get => _vm.NewAreaName;
        set => _vm.NewAreaName = value;
    }

    protected override void OnInitialized()
    {
        _vm = new AreasViewModel(AdminAreaService);
    }

    protected override async Task OnInitializedAsync()
    {
        await _vm.LoadAsync();
    }

    private async Task CreateArea()
    {
        var result = await _vm.CreateAsync();
        NotifyUser(result);

        if (result.RequiresReload)
        {
            await _vm.LoadAsync();
            await grid.Reload();
        }
    }

    private async Task OnRowUpdate(AreaAdminDto area)
    {
        var result = await _vm.UpdateAsync(area);
        NotifyUser(result);

        if (result.Outcome == AreasVmOutcome.ValidationError)
        {
            grid.CancelEditRow(area);
        }

        if (result.RequiresReload)
        {
            await _vm.LoadAsync();
        }
    }

    private async Task DeleteArea(int id)
    {
        var confirm = await DialogService.Confirm("¿Eliminar el área?", "Confirmar", new ConfirmOptions { OkButtonText = "Eliminar", CancelButtonText = "Cancelar", Icon = "warning" });
        if (confirm != true)
        {
            return;
        }

        var result = await _vm.DeleteAsync(id);
        NotifyUser(result);

        if (result.RequiresReload)
        {
            await _vm.LoadAsync();
            await grid.Reload();
        }
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

    private async Task OnSubmitArea()
    {
        newArea = areaForm.Name;
        await CreateArea();
        areaForm = new AreaFormModel();
    }
}

public sealed class AreaFormModel
{
    [Required(ErrorMessage = "Nombre requerido")]
    public string Name { get; set; } = string.Empty;
}
