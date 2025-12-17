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

    public static RoutesVmResult Success(string title, string message, bool requiresReload = true) =>
        new() { Outcome = RoutesVmOutcome.Success, Title = title, Message = message, RequiresReload = requiresReload };

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
    public bool Editing { get; private set; }
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

        if (Editing)
        {
            if (!Areas.Any(a => a.Id == SelectedAreaId))
                SelectedAreaId = Areas.FirstOrDefault()?.Id ?? 0;

            if (!Clients.Any(c => c.Id == SelectedClientId))
                SelectedClientId = Clients.FirstOrDefault()?.Id ?? 0;
        }
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
        Editing = true;
    }

    public void BeginEdit(AreaRouteAdminDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        EditModel = new AreaRouteAdminDto { Id = dto.Id };
        SelectedAreaId = dto.AreaId > 0
            ? dto.AreaId
            : Areas.FirstOrDefault()?.Id ?? 0;

        SelectedClientId = dto.ClientId > 0
            ? dto.ClientId
            : Clients.FirstOrDefault()?.Id ?? 0;
        EditReturnUrl = dto.ReturnUrl ?? string.Empty;
        EditPriority = dto.Priority > 0 ? dto.Priority : 1;
        EditIsActive = dto.IsActive;
        ValidationError = null;
        Editing = true;
    }

    public async Task<RoutesVmResult> SaveAsync()
    {
        ValidationError = null;

        if (SelectedAreaId <= 0 || SelectedClientId <= 0)
        {
            ValidationError = "Completa todos los campos";
            return RoutesVmResult.ValidationFailed("Validación", ValidationError);
        }

        var normalizedUrl = (EditReturnUrl ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedUrl))
        {
            ValidationError = "Completa todos los campos";
            return RoutesVmResult.ValidationFailed("Validación", ValidationError);
        }

        var priority = EditPriority > 0 ? EditPriority : 1;
        EditPriority = priority;
        EditReturnUrl = normalizedUrl;

        try
        {
            if (EditModel.Id == 0)
            {
                var id = await _routingService.CreateRouteAsync(SelectedAreaId, SelectedClientId, normalizedUrl, priority, EditIsActive);
                Editing = false;
                ValidationError = null;
                return RoutesVmResult.Success("Ruta creada", $"Id {id} creada.");
            }

            await _routingService.UpdateRouteAsync(EditModel.Id, SelectedAreaId, SelectedClientId, normalizedUrl, priority, EditIsActive);
            Editing = false;
            ValidationError = null;
            return RoutesVmResult.Success("Ruta actualizada", $"Id {EditModel.Id} actualizada.");
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
            return RoutesVmResult.Success("Ruta eliminada", $"Id {id} eliminada.");
        }
        catch (Exception ex)
        {
            return RoutesVmResult.Failed("Error al eliminar ruta", ex.Message);
        }
    }

    public void CancelEdit()
    {
        Editing = false;
        ValidationError = null;
    }
}
