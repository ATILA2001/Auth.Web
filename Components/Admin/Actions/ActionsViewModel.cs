using Auth.Web.Application.Admin.Dtos;
using Auth.Web.Services.Abstractions.Admin;

namespace Auth.Web.Components.Admin.Actions;

public enum ActionsVmOutcome
{
    Success,
    ValidationError,
    Error
}

public sealed class ActionsVmResult
{
    public ActionsVmOutcome Outcome { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public bool RequiresReload { get; init; }
    public int? CreatedId { get; init; }

    public static ActionsVmResult Success(string title, string message, bool requiresReload = true) =>
        new() { Outcome = ActionsVmOutcome.Success, Title = title, Message = message, RequiresReload = requiresReload };

    public static ActionsVmResult CreateSuccess(string title, string message, int createdId) =>
        new() { Outcome = ActionsVmOutcome.Success, Title = title, Message = message, RequiresReload = false, CreatedId = createdId };

    public static ActionsVmResult ValidationFailed(string title, string message) =>
        new() { Outcome = ActionsVmOutcome.ValidationError, Title = title, Message = message, RequiresReload = false };

    public static ActionsVmResult Failed(string title, string message) =>
        new() { Outcome = ActionsVmOutcome.Error, Title = title, Message = message, RequiresReload = false };
}

public sealed class ActionsViewModel
{
    private readonly IAdminActionPermissionService _actionService;

    public ActionsViewModel(IAdminActionPermissionService actionService)
    {
        ArgumentNullException.ThrowIfNull(actionService);
        _actionService = actionService;
    }

    public List<ActionPermissionAdminDto> Actions { get; private set; } = new();
    public ActionPermissionAdminDto EditModel { get; private set; } = new() { Id = 0, Name = string.Empty, UsageCount = 0 };
    public string EditName { get; set; } = string.Empty;
    public string? ValidationError { get; private set; }

    public async Task LoadAsync()
    {
        Actions = (await _actionService.GetActionsAsync()).ToList();
    }

    public void BeginCreate()
    {
        EditModel = new ActionPermissionAdminDto { Id = 0, Name = string.Empty, UsageCount = 0 };
        EditName = string.Empty;
        ValidationError = null;
    }

    public void BeginEdit(ActionPermissionAdminDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        EditModel = new ActionPermissionAdminDto { Id = dto.Id, Name = dto.Name, UsageCount = dto.UsageCount };
        EditName = dto.Name ?? string.Empty;
        ValidationError = null;
    }

    public ActionsVmResult ValidateOnly(string name)
    {
        ValidationError = null;
        var trimmedName = (name ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            ValidationError = "El nombre de la acción no puede estar vacío.";
            return ActionsVmResult.ValidationFailed("Validación", ValidationError);
        }

        var duplicate = Actions.Any(a => a.Id != EditModel.Id && string.Equals(a.Name, trimmedName, StringComparison.OrdinalIgnoreCase));
        if (duplicate)
        {
            ValidationError = "Ya existe una acción con ese nombre.";
            return ActionsVmResult.ValidationFailed("Validación", ValidationError);
        }

        return ActionsVmResult.Success("Válido", "", requiresReload: false);
    }

    public async Task<ActionsVmResult> SaveAsync()
    {
        // Reuse validation logic to avoid duplication and rule drift
        var validationResult = ValidateOnly(EditName);
        if (validationResult.Outcome != ActionsVmOutcome.Success)
        {
            return validationResult;
        }

        var name = EditName.Trim();

        try
        {
            if (EditModel.Id == 0)
            {
                // CREATE: return CreatedId so UI can set it locally without reload
                var id = await _actionService.CreateActionAsync(name);
                if (id != 0)
                {
                    ValidationError = null;
                    return ActionsVmResult.CreateSuccess("Acción creada", $"Se creó '{name}'.", id);
                }
                ValidationError = "Nombre inválido o duplicado.";
                return ActionsVmResult.ValidationFailed("Sin cambios", ValidationError);
            }

            // UPDATE: no reload required; buffer?DTO sync handles display update
            await _actionService.UpdateActionAsync(EditModel.Id, name);
            ValidationError = null;
            return ActionsVmResult.Success("Acción actualizada", $"Se actualizó '{name}'.", requiresReload: false);
        }
        catch (Exception ex)
        {
            ValidationError = ex.Message;
            return ActionsVmResult.Failed("Error al guardar acción", ex.Message);
        }
    }

    public async Task<ActionsVmResult> DeleteAsync(int id)
    {
        try
        {
            await _actionService.DeleteActionAsync(id);
            // DELETE: reload required to remove row from grid
            return ActionsVmResult.Success("Acción eliminada", $"Id {id} removido.", requiresReload: true);
        }
        catch (Exception ex)
        {
            return ActionsVmResult.Failed("Error al eliminar acción", ex.Message);
        }
    }

    // Legacy: Editing property and CancelEdit method not used by current UI
    // Code-behind calls grid.CancelEditRow directly; consider removal in future cleanup
}
