using Microsoft.AspNetCore.Components;
using Auth.Web.Application.Admin.Abstractions;
using Auth.Web.Application.Admin.Dtos;
using Radzen;

namespace Auth.Web.Components.Admin;

public partial class RoutesAdmin : ComponentBase
{
    [Inject] private IAdminRoutingService AdminRouting { get; set; } = default!;
    [Inject] private IAdminAreaService AdminAreas { get; set; } = default!;
    [Inject] private IAdminClientService AdminClients { get; set; } = default!;
    [Inject] private NotificationService Notifications { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private List<AreaRouteAdminDto> routes = new();
    private List<AreaAdminDto> areas = new();
    private List<ApplicationClientAdminDto> clients = new();
    private bool editing;
    private AreaRouteAdminDto editModel = new();
    private int selectedAreaId;
    private int selectedClientId;
    private string editReturnUrl = string.Empty;
    private int editPriority = 1;
    private bool editIsActive = true;
    private string? validationError;

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        routes = (await AdminRouting.GetRoutesAsync()).ToList();
        areas = (await AdminAreas.GetAreasAsync()).ToList();
        clients = (await AdminClients.GetClientsAsync()).ToList();
    }

    private void BeginCreate()
    {
        editModel = new AreaRouteAdminDto { Id = 0 };
        selectedAreaId = areas.FirstOrDefault()?.Id ?? 0;
        selectedClientId = clients.FirstOrDefault()?.Id ?? 0;
        editReturnUrl = string.Empty;
        editPriority = 1;
        editIsActive = true;
        validationError = null;
        editing = true;
    }

    private void BeginEdit(AreaRouteAdminDto dto)
    {
        editModel = dto;
        selectedAreaId = dto.AreaId;
        selectedClientId = dto.ClientId;
        editReturnUrl = dto.ReturnUrl;
        editPriority = dto.Priority;
        editIsActive = dto.IsActive;
        validationError = null;
        editing = true;
    }

    private async Task SaveRoute()
    {
        validationError = null;
        if (selectedAreaId == 0 || selectedClientId == 0 || string.IsNullOrWhiteSpace(editReturnUrl))
        {
            validationError = "Completa todos los campos";
            return;
        }
        try
        {
            if (editModel.Id == 0)
            {
                var id = await AdminRouting.CreateRouteAsync(selectedAreaId, selectedClientId, editReturnUrl, editPriority, editIsActive);
                Notifications.Notify(NotificationSeverity.Success, "Ruta creada", $"Id {id} creada.");
            }
            else
            {
                await AdminRouting.UpdateRouteAsync(editModel.Id, selectedAreaId, selectedClientId, editReturnUrl, editPriority, editIsActive);
                Notifications.Notify(NotificationSeverity.Success, "Ruta actualizada", $"Id {editModel.Id} actualizada.");
            }
            editing = false;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            validationError = ex.Message;
            Notifications.Notify(NotificationSeverity.Error, "Error al guardar ruta", ex.Message);
        }
    }

    private async Task DeleteRoute(int id)
    {
        try
        {
            await AdminRouting.DeleteRouteAsync(id);
            Notifications.Notify(NotificationSeverity.Success, "Ruta eliminada", $"Id {id} eliminada.");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            Notifications.Notify(NotificationSeverity.Error, "Error al eliminar ruta", ex.Message);
        }
    }

    private void CancelEdit() => editing = false;
}
