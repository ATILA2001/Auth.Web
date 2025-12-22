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

    public static ActionsVmResult Success(string title, string message, bool requiresReload = true) =>
        new() { Outcome = ActionsVmOutcome.Success, Title = title, Message = message, RequiresReload = requiresReload };

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
    public string NewActionName { get; set; } = string.Empty;

    public async Task LoadAsync()
    {
        Actions = (await _actionService.GetActionsAsync()).ToList();
    }

    public async Task<ActionsVmResult> CreateAsync()
    {
        var name = NewActionName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name))
        {
            return ActionsVmResult.ValidationFailed("Validación", "El nombre de la acción no puede estar vacío.");
        }

        if (Actions.Any(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            return ActionsVmResult.ValidationFailed("Validación", "Ya existe una acción con ese nombre.");
        }

        try
        {
            var id = await _actionService.CreateActionAsync(name);
            if (id != 0)
            {
                NewActionName = string.Empty;
                return ActionsVmResult.Success("Acción creada", $"Se creó '{name}' correctamente.");
            }
            else
            {
                return ActionsVmResult.ValidationFailed("Sin cambios", "Nombre inválido o duplicado.");
            }
        }
        catch (Exception ex)
        {
            return ActionsVmResult.Failed("Error al crear acción", ex.Message);
        }
    }

    public async Task<ActionsVmResult> UpdateAsync(ActionPermissionAdminDto action)
    {
        var name = action.Name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name))
        {
            return ActionsVmResult.ValidationFailed("Validación", "El nombre de la acción no puede estar vacío.");
        }

        if (Actions.Any(a => a.Id != action.Id && string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            return ActionsVmResult.ValidationFailed("Validación", "Ya existe una acción con ese nombre.");
        }

        try
        {
            await _actionService.UpdateActionAsync(action.Id, name);
            return ActionsVmResult.Success("Acción actualizada", $"Se actualizó '{name}'.");
        }
        catch (Exception ex)
        {
            return ActionsVmResult.Failed("Error al actualizar acción", ex.Message);
        }
    }

    public async Task<ActionsVmResult> DeleteAsync(int id)
    {
        try
        {
            await _actionService.DeleteActionAsync(id);
            return ActionsVmResult.Success("Acción eliminada", $"Id {id} removido.");
        }
        catch (Exception ex)
        {
            return ActionsVmResult.Failed("Error al eliminar acción", ex.Message);
        }
    }
}
