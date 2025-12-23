using Microsoft.AspNetCore.Components;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Application.Admin.Dtos;
using Radzen;
using System.ComponentModel.DataAnnotations;

namespace Auth.Web.Components.Admin.Areas;

public partial class Areas : ComponentBase
{
    [Inject] private IAdminAreaService AdminAreaService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;

    private AreasViewModel _vm = null!;
    private AreaFormModel areaForm = new();

    private List<AreaAdminDto> areas => _vm.Areas;
    private AreaAdminDto editModel => _vm.EditModel;
    private bool editing => _vm.Editing;
    private string editName
    {
        get => _vm.EditName;
        set => _vm.EditName = value;
    }
    private string? validationError => _vm.ValidationError;

    protected override void OnInitialized()
    {
        _vm = new AreasViewModel(AdminAreaService);
    }

    protected override async Task OnInitializedAsync()
    {
        await _vm.LoadAsync();
    }

    private void BeginCreate()
    {
        _vm.BeginCreate();
        areaForm = new AreaFormModel();
    }

    private void BeginEdit(AreaAdminDto dto)
    {
        _vm.BeginEdit(dto);
        areaForm = new AreaFormModel { Name = editName };
    }

    private async Task OnSubmitArea()
    {
        editName = areaForm.Name;
        var result = await _vm.SaveAsync();
        NotifyUser(result);

        if (result.RequiresReload)
        {
            await _vm.LoadAsync();
        }

        if (result.Outcome != AreasVmOutcome.ValidationError)
        {
            _vm.CancelEdit();
            areaForm = new AreaFormModel();
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
        }
    }

    private void CancelEdit()
    {
        _vm.CancelEdit();
        areaForm = new AreaFormModel();
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

public sealed class AreaFormModel
{
    [Required(ErrorMessage = "Nombre requerido")]
    public string Name { get; set; } = string.Empty;
}
