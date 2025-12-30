using Auth.Web.Application.Admin.Dtos;
using Auth.Web.Services.Abstractions.Admin;

namespace Auth.Web.Components.Admin.Routes;

public enum RoutesVmOutcome
{
    Success,
    ValidationError,
    Error
}

public sealed class RoutesVmResult
{
    public RoutesVmOutcome Outcome { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public bool RequiresReload { get; init; }
    public int? CreatedId { get; init; }

    public static RoutesVmResult Success(string title, string message, bool requiresReload = true) =>
        new() { Outcome = RoutesVmOutcome.Success, Title = title, Message = message, RequiresReload = requiresReload };

    public static RoutesVmResult CreateSuccess(string title, string message, int createdId) =>
        new() { Outcome = RoutesVmOutcome.Success, Title = title, Message = message, RequiresReload = false, CreatedId = createdId };

    public static RoutesVmResult ValidationFailed(string title, string message) =>
        new() { Outcome = RoutesVmOutcome.ValidationError, Title = title, Message = message, RequiresReload = false };

    public static RoutesVmResult Failed(string title, string message) =>
        new() { Outcome = RoutesVmOutcome.Error, Title = title, Message = message, RequiresReload = false };
}

public sealed class RoutesViewModel
{
    private readonly IAdminRoutingService _routingService;
    private readonly IAdminAreaService _areaService;
    private readonly IAdminClientService _clientService;

    public RoutesViewModel(IAdminRoutingService routingService, IAdminAreaService areaService, IAdminClientService clientService)
    {
        ArgumentNullException.ThrowIfNull(routingService);
        ArgumentNullException.ThrowIfNull(areaService);
        ArgumentNullException.ThrowIfNull(clientService);
        _routingService = routingService;
        _areaService = areaService;
        _clientService = clientService;
    }

    public List<AreaRouteAdminDto> Routes { get; private set; } = new();
    public List<AreaAdminDto> Areas { get; private set; } = new();
    public List<ApplicationClientAdminDto> Clients { get; private set; } = new();
    public AreaRouteAdminDto EditModel { get; private set; } = new();
    public int SelectedAreaId { get; set; }
    public int SelectedClientId { get; set; }
    public string EditReturnUrl { get; set; } = string.Empty;
    public int EditPriority { get; set; } = 1;
    public bool EditIsActive { get; set; } = true;
    public string? ValidationError { get; private set; }

    public async Task LoadAsync()
    {
        Routes = (await _routingService.GetRoutesAsync()).ToList();
        Areas = (await _areaService.GetAreasAsync()).ToList();
        Clients = (await _clientService.GetClientsAsync()).ToList();
    }

    public void BeginCreate()
    {
        EditModel = new AreaRouteAdminDto { Id = 0 };
        SelectedAreaId = Areas.FirstOrDefault()?.Id ?? 0;
        SelectedClientId = Clients.FirstOrDefault()?.Id ?? 0;
        EditReturnUrl = string.Empty;
        EditPriority = 1;
        EditIsActive = true;
        ValidationError = null;
    }

    public void BeginEdit(AreaRouteAdminDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        EditModel = new AreaRouteAdminDto { Id = dto.Id };
        SelectedAreaId = dto.AreaId > 0 ? dto.AreaId : Areas.FirstOrDefault()?.Id ?? 0;
        SelectedClientId = dto.ClientId > 0 ? dto.ClientId : Clients.FirstOrDefault()?.Id ?? 0;
        EditReturnUrl = dto.ReturnUrl ?? string.Empty;
        EditPriority = dto.Priority > 0 ? dto.Priority : 1;
        EditIsActive = dto.IsActive;
        ValidationError = null;
    }

    public RoutesVmResult ValidateOnly(int areaId, int clientId, string returnUrl, int priority)
    {
        ValidationError = null;

        if (areaId <= 0)
        {
            ValidationError = "Debe seleccionar un área.";
            return RoutesVmResult.ValidationFailed("Validación", ValidationError);
        }

        if (clientId <= 0)
        {
            ValidationError = "Debe seleccionar un cliente.";
            return RoutesVmResult.ValidationFailed("Validación", ValidationError);
        }

        var normalizedUrl = (returnUrl ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedUrl))
        {
            ValidationError = "La URL de retorno no puede estar vacía.";
            return RoutesVmResult.ValidationFailed("Validación", ValidationError);
        }

        if (priority <= 0)
        {
            ValidationError = "La prioridad debe ser mayor a 0.";
            return RoutesVmResult.ValidationFailed("Validación", ValidationError);
        }

        return RoutesVmResult.Success("Válido", "", requiresReload: false);
    }

    public async Task<RoutesVmResult> SaveAsync()
    {
        // Reuse validation logic to avoid duplication and rule drift
        var validationResult = ValidateOnly(SelectedAreaId, SelectedClientId, EditReturnUrl, EditPriority);
        if (validationResult.Outcome != RoutesVmOutcome.Success)
        {
            return validationResult;
        }

        var normalizedUrl = EditReturnUrl.Trim();
        var priority = EditPriority > 0 ? EditPriority : 1;

        try
        {
            if (EditModel.Id == 0)
            {
                // CREATE: return CreatedId so UI can set it locally without reload
                var id = await _routingService.CreateRouteAsync(SelectedAreaId, SelectedClientId, normalizedUrl, priority, EditIsActive);
                if (id != 0)
                {
                    ValidationError = null;
                    return RoutesVmResult.CreateSuccess("Ruta creada", $"Se creó la ruta.", id);
                }
                ValidationError = "No se pudo crear la ruta.";
                return RoutesVmResult.ValidationFailed("Sin cambios", ValidationError);
            }

            // UPDATE: no reload required; buffer?DTO sync handles display update
            await _routingService.UpdateRouteAsync(EditModel.Id, SelectedAreaId, SelectedClientId, normalizedUrl, priority, EditIsActive);
            ValidationError = null;
            return RoutesVmResult.Success("Ruta actualizada", $"Se actualizó la ruta.", requiresReload: false);
        }
        catch (Exception ex)
        {
            ValidationError = ex.Message;
            return RoutesVmResult.Failed("Error al guardar ruta", ex.Message);
        }
    }

    public async Task<RoutesVmResult> DeleteAsync(int id)
    {
        try
        {
            await _routingService.DeleteRouteAsync(id);
            // DELETE: reload required to remove row from grid
            return RoutesVmResult.Success("Ruta eliminada", $"Id {id} removido.", requiresReload: true);
        }
        catch (Exception ex)
        {
            return RoutesVmResult.Failed("Error al eliminar ruta", ex.Message);
        }
    }

    // Legacy: Editing property and CancelEdit method not used by current UI
    // Code-behind calls grid.CancelEditRow directly; consider removal in future cleanup
}
