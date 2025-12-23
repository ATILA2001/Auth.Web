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
    public AreaAdminDto EditModel { get; private set; } = new() { Id = 0, Name = string.Empty, UserCount = 0 };
    public string EditName { get; set; } = string.Empty;
    public bool Editing { get; private set; }
    public string? ValidationError { get; private set; }

    public async Task LoadAsync()
    {
        Areas = (await _areaService.GetAreasAsync()).ToList();
    }

    public void BeginCreate()
    {
        EditModel = new AreaAdminDto { Id = 0, Name = string.Empty, UserCount = 0 };
        EditName = string.Empty;
        ValidationError = null;
        Editing = true;
    }

    public void BeginEdit(AreaAdminDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        EditModel = new AreaAdminDto { Id = dto.Id, Name = dto.Name, UserCount = dto.UserCount };
        EditName = dto.Name ?? string.Empty;
        ValidationError = null;
        Editing = true;
    }

    public async Task<AreasVmResult> SaveAsync()
    {
        ValidationError = null;
        var name = (EditName ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            ValidationError = "El nombre del área no puede estar vacío.";
            return AreasVmResult.ValidationFailed("Validación", ValidationError);
        }

        var duplicate = Areas.Any(a => a.Id != EditModel.Id && string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));
        if (duplicate)
        {
            ValidationError = "Ya existe un área con ese nombre.";
            return AreasVmResult.ValidationFailed("Validación", ValidationError);
        }

        try
        {
            if (EditModel.Id == 0)
            {
                var id = await _areaService.CreateAreaAsync(name);
                if (id != 0)
                {
                    Editing = false;
                    ValidationError = null;
                    return AreasVmResult.Success("Área creada", $"Se creó '{name}'.");
                }
                ValidationError = "Nombre inválido o duplicado.";
                return AreasVmResult.ValidationFailed("Sin cambios", ValidationError);
            }

            await _areaService.UpdateAreaAsync(EditModel.Id, name);
            Editing = false;
            ValidationError = null;
            return AreasVmResult.Success("Área actualizada", $"Se actualizó '{name}'.");
        }
        catch (Exception ex)
        {
            ValidationError = ex.Message;
            return AreasVmResult.Failed("Error al guardar área", ex.Message);
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

    public void CancelEdit()
    {
        Editing = false;
        ValidationError = null;
        EditName = string.Empty;
        EditModel = new AreaAdminDto { Id = 0, Name = string.Empty, UserCount = 0 };
    }
}
