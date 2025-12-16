using Auth.Web.Application.Admin.Dtos;
using Auth.Web.Services.Abstractions.Admin;

namespace Auth.Web.Components.Admin.Areas;

public enum AreasVmOutcome
{
    Success,
    ValidationError,
    Error
}

public sealed class AreasVmResult
{
    public AreasVmOutcome Outcome { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public bool RequiresReload { get; init; }

    public static AreasVmResult Success(string title, string message, bool requiresReload = true) =>
        new() { Outcome = AreasVmOutcome.Success, Title = title, Message = message, RequiresReload = requiresReload };

    public static AreasVmResult ValidationFailed(string title, string message) =>
        new() { Outcome = AreasVmOutcome.ValidationError, Title = title, Message = message, RequiresReload = false };

    public static AreasVmResult Failed(string title, string message) =>
        new() { Outcome = AreasVmOutcome.Error, Title = title, Message = message, RequiresReload = false };
}

public sealed class AreasViewModel
{
    private readonly IAdminAreaService _areaService;

    public AreasViewModel(IAdminAreaService areaService)
    {
        ArgumentNullException.ThrowIfNull(areaService);
        _areaService = areaService;
    }

    public List<AreaAdminDto> Areas { get; private set; } = new();
    public string NewAreaName { get; set; } = string.Empty;

    public async Task LoadAsync()
    {
        Areas = (await _areaService.GetAreasAsync()).ToList();
    }

    public async Task<AreasVmResult> CreateAsync()
    {
        var name = NewAreaName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name))
        {
            return AreasVmResult.ValidationFailed("Validación", "El nombre del área no puede estar vacío.");
        }

        try
        {
            var id = await _areaService.CreateAreaAsync(name);
            if (id != 0)
            {
                NewAreaName = string.Empty;
                return AreasVmResult.Success("Área creada", $"Se creó '{name}' correctamente.");
            }
            else
            {
                return AreasVmResult.ValidationFailed("Sin cambios", "Nombre inválido o duplicado.");
            }
        }
        catch (Exception ex)
        {
            return AreasVmResult.Failed("Error al crear área", ex.Message);
        }
    }

    public async Task<AreasVmResult> UpdateAsync(AreaAdminDto area)
    {
        var name = area.Name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name))
        {
            return AreasVmResult.ValidationFailed("Validación", "El nombre del área no puede estar vacío.");
        }

        try
        {
            await _areaService.UpdateAreaAsync(area.Id, name);
            return AreasVmResult.Success("Área actualizada", $"Se actualizó '{name}'.");
        }
        catch (Exception ex)
        {
            return AreasVmResult.Failed("Error al actualizar área", ex.Message);
        }
    }

    public async Task<AreasVmResult> DeleteAsync(int id)
    {
        try
        {
            await _areaService.DeleteAreaAsync(id);
            return AreasVmResult.Success("Área eliminada", $"Id {id} removido.");
        }
        catch (Exception ex)
        {
            return AreasVmResult.Failed("Error al eliminar área", ex.Message);
        }
    }
}
