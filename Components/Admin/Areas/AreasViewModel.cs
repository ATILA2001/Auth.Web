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
    public int? CreatedId { get; init; }

    public static AreasVmResult Success(string title, string message, bool requiresReload = true) =>
        new() { Outcome = AreasVmOutcome.Success, Title = title, Message = message, RequiresReload = requiresReload };

    public static AreasVmResult CreateSuccess(string title, string message, int createdId) =>
        new() { Outcome = AreasVmOutcome.Success, Title = title, Message = message, RequiresReload = false, CreatedId = createdId };

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
    }

    public void BeginEdit(AreaAdminDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        EditModel = new AreaAdminDto { Id = dto.Id, Name = dto.Name, UserCount = dto.UserCount };
        EditName = dto.Name ?? string.Empty;
        ValidationError = null;
    }

    public AreasVmResult ValidateOnly(string name)
    {
        ValidationError = null;
        var trimmedName = (name ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            ValidationError = "El nombre del área no puede estar vacío.";
            return AreasVmResult.ValidationFailed("Validación", ValidationError);
        }

        var duplicate = Areas.Any(a => a.Id != EditModel.Id && string.Equals(a.Name, trimmedName, StringComparison.OrdinalIgnoreCase));
        if (duplicate)
        {
            ValidationError = "Ya existe un área con ese nombre.";
            return AreasVmResult.ValidationFailed("Validación", ValidationError);
        }

        return AreasVmResult.Success("Válido", "", requiresReload: false);
    }

    public async Task<AreasVmResult> SaveAsync()
    {
        // Reuse validation logic to avoid duplication and rule drift
        var validationResult = ValidateOnly(EditName);
        if (validationResult.Outcome != AreasVmOutcome.Success)
        {
            return validationResult;
        }

        var name = EditName.Trim();

        try
        {
            if (EditModel.Id == 0)
            {
                // CREATE: return CreatedId so UI can set it locally without reload
                var id = await _areaService.CreateAreaAsync(name);
                if (id != 0)
                {
                    ValidationError = null;
                    return AreasVmResult.CreateSuccess("Área creada", $"Se creó '{name}'.", id);
                }
                ValidationError = "Nombre inválido o duplicado.";
                return AreasVmResult.ValidationFailed("Sin cambios", ValidationError);
            }

            // UPDATE: no reload required; buffer?DTO sync handles display update
            await _areaService.UpdateAreaAsync(EditModel.Id, name);
            ValidationError = null;
            return AreasVmResult.Success("Área actualizada", $"Se actualizó '{name}'.", requiresReload: false);
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
            // DELETE: reload required to remove row from grid
            return AreasVmResult.Success("Área eliminada", $"Id {id} removido.", requiresReload: true);
        }
        catch (Exception ex)
        {
            return AreasVmResult.Failed("Error al eliminar área", ex.Message);
        }
    }

    // Legacy: Editing property and CancelEdit method not used by current UI
    // Code-behind calls grid.CancelEditRow directly; consider removal in future cleanup
}
