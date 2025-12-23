using Microsoft.AspNetCore.Components;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Application.Admin.Dtos;
using Radzen;
using System.ComponentModel.DataAnnotations;

namespace Auth.Web.Components.Admin.Actions;

public partial class Actions : ComponentBase
{
    [Inject] private IAdminActionPermissionService AdminActionService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;

    private ActionsViewModel _vm = null!;
    private ActionFormModel actionForm = new();

    private List<ActionPermissionAdminDto> actions => _vm.Actions;
    private ActionPermissionAdminDto editModel => _vm.EditModel;
    private bool editing => _vm.Editing;
    private string editName
    {
        get => _vm.EditName;
        set => _vm.EditName = value;
    }
    private string? validationError => _vm.ValidationError;

    protected override void OnInitialized()
    {
        _vm = new ActionsViewModel(AdminActionService);
    }

    protected override async Task OnInitializedAsync()
    {
        await _vm.LoadAsync();
    }

    private void BeginCreate()
    {
        _vm.BeginCreate();
        actionForm = new ActionFormModel();
    }

    private void BeginEdit(ActionPermissionAdminDto action)
    {
        _vm.BeginEdit(action);
        actionForm = new ActionFormModel { Name = editName };
    }

    private async Task OnSubmitAction()
    {
        editName = actionForm.Name;
        var result = await _vm.SaveAsync();
        NotifyUser(result);

        if (result.RequiresReload)
        {
            await _vm.LoadAsync();
        }

        if (result.Outcome != ActionsVmOutcome.ValidationError)
        {
            _vm.CancelEdit();
            actionForm = new ActionFormModel();
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
        }
    }

    private void CancelEdit()
    {
        _vm.CancelEdit();
        actionForm = new ActionFormModel();
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

public sealed class ActionFormModel
{
    [Required(ErrorMessage = "Nombre requerido")]
    public string Name { get; set; } = string.Empty;
}
