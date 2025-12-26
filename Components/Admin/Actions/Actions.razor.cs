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
    private string editName
    {
        get => _vm.EditName;
        set => _vm.EditName = value;
    }

    protected override void OnInitialized()
    {
        _vm = new ActionsViewModel(AdminActionService);
    }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await _vm.LoadAsync();
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "No se pudieron cargar las acciones.", ex.Message);
        }
    }

    private async Task BeginCreate()
    {
        _vm.BeginCreate();
        var newAction = new ActionPermissionAdminDto { Id = 0, Name = string.Empty, UsageCount = 0 };
        actions.Insert(0, newAction);
        await grid.InsertRow(newAction);
    }

    private async Task OnRowCreate(ActionPermissionAdminDto action)
    {
        _vm.BeginCreate();
        editName = action.Name;
        var result = await _vm.SaveAsync();
        NotifyUser(result);

        if (result.RequiresReload)
        {
            await _vm.LoadAsync();
            await grid.Reload();
        }

        if (result.Outcome == ActionsVmOutcome.ValidationError)
        {
            actions.Remove(action);
            await grid.Reload();
        }
    }

    private async Task EditRow(ActionPermissionAdminDto action)
    {
        await grid.EditRow(action);
    }

    private async Task OnRowUpdate(ActionPermissionAdminDto action)
    {
        _vm.BeginEdit(action);
        editName = action.Name;
        var result = await _vm.SaveAsync();
        NotifyUser(result);

        if (result.RequiresReload)
        {
            await _vm.LoadAsync();
            await grid.Reload();
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

    private void CancelEditRow(ActionPermissionAdminDto action)
    {
        grid.CancelEditRow(action);
        if (action.Id == 0)
        {
            actions.Remove(action);
        }
    }

    private void ClearFilters()
    {
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
