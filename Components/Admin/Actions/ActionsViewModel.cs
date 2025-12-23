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
    public ActionPermissionAdminDto EditModel { get; private set; } = new() { Id = 0, Name = string.Empty, UsageCount = 0 };
    public string EditName { get; set; } = string.Empty;
    public bool Editing { get; private set; }
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
        Editing = true;
    }

    public void BeginEdit(ActionPermissionAdminDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        EditModel = new ActionPermissionAdminDto { Id = dto.Id, Name = dto.Name, UsageCount = dto.UsageCount };
        EditName = dto.Name ?? string.Empty;
        ValidationError = null;
        Editing = true;
    }

    public async Task<ActionsVmResult> SaveAsync()
    {
        ValidationError = null;
        var name = (EditName ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            ValidationError = "El nombre de la acción no puede estar vacío.";
            return ActionsVmResult.ValidationFailed("Validación", ValidationError);
        }

        var duplicate = Actions.Any(a => a.Id != EditModel.Id && string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));
        if (duplicate)
        {
            ValidationError = "Ya existe una acción con ese nombre.";
            return ActionsVmResult.ValidationFailed("Validación", ValidationError);
        }

        try
        {
            if (EditModel.Id == 0)
            {
                var id = await _actionService.CreateActionAsync(name);
                if (id != 0)
                {
                    Editing = false;
                    ValidationError = null;
                    return ActionsVmResult.Success("Acción creada", $"Se creó '{name}' correctamente.");
                }

                ValidationError = "Nombre inválido o duplicado.";
                return ActionsVmResult.ValidationFailed("Sin cambios", ValidationError);
            }

            await _actionService.UpdateActionAsync(EditModel.Id, name);
            Editing = false;
            ValidationError = null;
            return ActionsVmResult.Success("Acción actualizada", $"Se actualizó '{name}'.");
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
            return ActionsVmResult.Success("Acción eliminada", $"Id {id} removido.");
        }
        catch (Exception ex)
        {
            return ActionsVmResult.Failed("Error al eliminar acción", ex.Message);
        }
    }

    public void CancelEdit()
    {
        Editing = false;
        ValidationError = null;
        EditName = string.Empty;
        EditModel = new ActionPermissionAdminDto { Id = 0, Name = string.Empty, UsageCount = 0 };
    }
}
