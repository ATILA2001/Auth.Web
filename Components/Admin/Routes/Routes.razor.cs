using Microsoft.AspNetCore.Components;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Application.Admin.Dtos;
using Radzen;

namespace Auth.Web.Components.Admin.Routes;

public partial class Routes : ComponentBase
{
    [Inject] private IAdminRoutingService AdminRouting { get; set; } = null!;
    [Inject] private IAdminAreaService AdminAreas { get; set; } = null!;
    [Inject] private IAdminClientService AdminClients { get; set; } = null!;
    [Inject] private NotificationService Notifications { get; set; } = null!;

    private RoutesViewModel _vm = null!;

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
    }

    private void BeginCreate() => _vm.BeginCreate();

    private void BeginEdit(AreaRouteAdminDto dto) => _vm.BeginEdit(dto);

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
}
