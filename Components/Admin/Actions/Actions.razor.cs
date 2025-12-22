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

    private List<ActionPermissionAdminDto> actions => _vm.Actions;
    private string newAction
    {
        get => _vm.NewActionName;
        set => _vm.NewActionName = value;
    }

    protected override void OnInitialized()
    {
        _vm = new ActionsViewModel(AdminActionService);
    }

    protected override async Task OnInitializedAsync()
    {
        await _vm.LoadAsync();
    }

    private async Task CreateAction()
    {
        var result = await _vm.CreateAsync();
        NotifyUser(result);

        if (result.RequiresReload)
        {
            await _vm.LoadAsync();
            await grid.Reload();
        }
    }

    private async Task OnRowUpdate(ActionPermissionAdminDto action)
    {
        var result = await _vm.UpdateAsync(action);
        NotifyUser(result);

        if (result.Outcome == ActionsVmOutcome.ValidationError)
        {
            grid.CancelEditRow(action);
        }

        if (result.RequiresReload)
        {
            await _vm.LoadAsync();
        }
    }

    private async Task DeleteAction(int id)
    {
        var confirm = await DialogService.Confirm("¿Eliminar la acción?", "Confirmar", new ConfirmOptions { OkButtonText = "Eliminar", CancelButtonText = "Cancelar", Icon = "warning" });
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
