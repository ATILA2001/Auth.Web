using Microsoft.AspNetCore.Components;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Application.Admin.Dtos;
using Radzen;
using Radzen.Blazor;
using System.ComponentModel.DataAnnotations;

namespace Auth.Web.Components.Admin.Routes;

public partial class Routes : ComponentBase
{
    [Inject] private IAdminRoutingService AdminRouting { get; set; } = null!;
    [Inject] private IAdminAreaService AdminAreas { get; set; } = null!;
    [Inject] private IAdminClientService AdminClients { get; set; } = null!;
    [Inject] private NotificationService Notifications { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;

    private RoutesViewModel _vm = null!;
    private RoutesFormModel routeForm = new();

    private List<AreaRouteAdminDto> routes => _vm.Routes;
    private List<AreaAdminDto> areas => _vm.Areas;
    private List<ApplicationClientAdminDto> clients => _vm.Clients;
    private bool editing => _vm.Editing;
    private AreaRouteAdminDto editModel => _vm.EditModel;
    private int selectedAreaId
    {
        get => _vm.SelectedAreaId;
        set => _vm.SelectedAreaId = value;
    }
    private int selectedClientId
    {
        get => _vm.SelectedClientId;
        set => _vm.SelectedClientId = value;
    }
    private string editReturnUrl
    {
        get => _vm.EditReturnUrl;
        set => _vm.EditReturnUrl = value;
    }
    private int editPriority
    {
        get => _vm.EditPriority;
        set => _vm.EditPriority = value;
    }
    private bool editIsActive
    {
        get => _vm.EditIsActive;
        set => _vm.EditIsActive = value;
    }
    private string? validationError => _vm.ValidationError;

    protected override void OnInitialized()
    {
        _vm = new RoutesViewModel(AdminRouting, AdminAreas, AdminClients);
    }

    protected override async Task OnInitializedAsync()
    {
        await _vm.LoadAsync();
        SyncFormFromVm();
    }

    private void BeginCreate()
    {
        _vm.BeginCreate();
        SyncFormFromVm();
    }

    private void BeginEdit(AreaRouteAdminDto dto)
    {
        _vm.BeginEdit(dto);
        SyncFormFromVm();
    }

    private async Task OnSubmitRoute()
    {
        _vm.SelectedAreaId = routeForm.AreaId;
        _vm.SelectedClientId = routeForm.ClientId;
        _vm.EditReturnUrl = routeForm.ReturnUrl;
        _vm.EditPriority = routeForm.Priority;
        _vm.EditIsActive = routeForm.IsActive;
        await SaveRoute();
    }

    private async Task SaveRoute()
    {
        var result = await _vm.SaveAsync();
        NotifyUser(result);

        if (result.RequiresReload)
        {
            await _vm.LoadAsync();
            StateHasChanged(); 
        }
    }

    private async Task DeleteRoute(int id)
    {
        var confirm = await DialogService.Confirm("¿Eliminar la ruta?", "Confirmar", new ConfirmOptions { OkButtonText = "Eliminar", CancelButtonText = "Cancelar", Icon = "warning" });
        if (confirm != true)
        {
            return;
        }

        var result = await _vm.DeleteAsync(id);
        NotifyUser(result);

        if (result.RequiresReload)
        {
            await _vm.LoadAsync();
            StateHasChanged(); 
        }
    }

    private void CancelEdit() => _vm.CancelEdit();

    private void NotifyUser(RoutesVmResult result)
    {
        var severity = result.Outcome switch
        {
            RoutesVmOutcome.Success => NotificationSeverity.Success,
            RoutesVmOutcome.ValidationError => NotificationSeverity.Warning,
            RoutesVmOutcome.Error => NotificationSeverity.Error,
            _ => NotificationSeverity.Info
        };

        Notifications.Notify(severity, result.Title, result.Message);
    }

    private void SyncFormFromVm()
    {
        routeForm = new RoutesFormModel
        {
            AreaId = _vm.SelectedAreaId,
            ClientId = _vm.SelectedClientId,
            ReturnUrl = _vm.EditReturnUrl,
            Priority = _vm.EditPriority,
            IsActive = _vm.EditIsActive
        };
    }
}

public sealed class RoutesFormModel
{
    [Required(ErrorMessage = "Área requerida")]
    [Range(1, int.MaxValue, ErrorMessage = "Área requerida")]
    public int AreaId { get; set; }

    [Required(ErrorMessage = "Cliente requerido")]
    [Range(1, int.MaxValue, ErrorMessage = "Cliente requerido")]
    public int ClientId { get; set; }

    [Required(ErrorMessage = "ReturnUrl requerida")]
    public string ReturnUrl { get; set; } = string.Empty;

    [Required(ErrorMessage = "Prioridad requerida")]
    [Range(1, int.MaxValue, ErrorMessage = "Prioridad requerida")]
    public int Priority { get; set; } = 1;

    public bool IsActive { get; set; } = true;
}
