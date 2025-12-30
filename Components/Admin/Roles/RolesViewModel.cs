using Auth.Web.Application.Admin.Dtos;
using Auth.Web.Services.Abstractions.Admin;

namespace Auth.Web.Components.Admin.Roles;

public enum RolesVmOutcome
{
    Success,
    ValidationError,
    Error
}

public sealed class RolesVmResult
{
    public RolesVmOutcome Outcome { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public bool RequiresReload { get; init; }
    public string? CreatedId { get; init; }

    public static RolesVmResult Success(string title, string message, bool requiresReload = true) =>
        new() { Outcome = RolesVmOutcome.Success, Title = title, Message = message, RequiresReload = requiresReload };

    public static RolesVmResult CreateSuccess(string title, string message, string createdId) =>
        new() { Outcome = RolesVmOutcome.Success, Title = title, Message = message, RequiresReload = false, CreatedId = createdId };

    public static RolesVmResult ValidationFailed(string title, string message) =>
        new() { Outcome = RolesVmOutcome.ValidationError, Title = title, Message = message, RequiresReload = false };

    public static RolesVmResult Failed(string title, string message) =>
        new() { Outcome = RolesVmOutcome.Error, Title = title, Message = message, RequiresReload = false };
}

public sealed class RolesViewModel
{
    private readonly IAdminRoleService _roleService;

    public RolesViewModel(IAdminRoleService roleService)
    {
        ArgumentNullException.ThrowIfNull(roleService);
        _roleService = roleService;
    }

    public List<RoleAdminDto> Roles { get; private set; } = new();
    public RoleAdminDto EditModel { get; private set; } = new() { Id = string.Empty, Name = string.Empty, UserCount = 0 };
    public string EditName { get; set; } = string.Empty;
    public string? ValidationError { get; private set; }

    public async Task LoadAsync()
    {
        Roles = (await _roleService.GetRolesAsync()).ToList();
    }

    public void BeginCreate()
    {
        EditModel = new RoleAdminDto { Id = string.Empty, Name = string.Empty, UserCount = 0 };
        EditName = string.Empty;
        ValidationError = null;
    }

    public void BeginEdit(RoleAdminDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        EditModel = new RoleAdminDto { Id = dto.Id, Name = dto.Name, UserCount = dto.UserCount };
        EditName = dto.Name ?? string.Empty;
        ValidationError = null;
    }

    public RolesVmResult ValidateOnly(string name)
    {
        ValidationError = null;
        var trimmedName = (name ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            ValidationError = "El nombre del rol no puede estar vacío.";
            return RolesVmResult.ValidationFailed("Validación", ValidationError);
        }

        var duplicate = Roles.Any(r => r.Id != EditModel.Id && string.Equals(r.Name, trimmedName, StringComparison.OrdinalIgnoreCase));
        if (duplicate)
        {
            ValidationError = "Ya existe un rol con ese nombre.";
            return RolesVmResult.ValidationFailed("Validación", ValidationError);
        }

        return RolesVmResult.Success("Válido", "", requiresReload: false);
    }

    public async Task<RolesVmResult> SaveAsync()
    {
        // Reuse validation logic to avoid duplication and rule drift
        var validationResult = ValidateOnly(EditName);
        if (validationResult.Outcome != RolesVmOutcome.Success)
        {
            return validationResult;
        }

        var name = EditName.Trim();

        try
        {
            if (string.IsNullOrWhiteSpace(EditModel.Id))
            {
                // CREATE: return CreatedId so UI can set it locally without reload
                var id = await _roleService.CreateRoleAsync(name);
                if (!string.IsNullOrEmpty(id))
                {
                    ValidationError = null;
                    return RolesVmResult.CreateSuccess("Rol creado", $"Se creó '{name}'.", id);
                }
                ValidationError = "Nombre inVálido o duplicado.";
                return RolesVmResult.ValidationFailed("Sin cambios", ValidationError);
            }

            // UPDATE: no reload required; buffer?DTO sync handles display update
            await _roleService.RenameRoleAsync(EditModel.Id, name);
            ValidationError = null;
            return RolesVmResult.Success("Rol actualizado", $"Se actualizó '{name}'.", requiresReload: false);
        }
        catch (Exception ex)
        {
            ValidationError = ex.Message;
            return RolesVmResult.Failed("Error al guardar rol", ex.Message);
        }
    }

    public async Task<RolesVmResult> DeleteAsync(string roleId)
    {
        try
        {
            await _roleService.DeleteRoleAsync(roleId);
            // DELETE: reload required to remove row from grid
            return RolesVmResult.Success("Rol eliminado", $"Id {roleId} removido.", requiresReload: true);
        }
        catch (Exception ex)
        {
            return RolesVmResult.Failed("Error al eliminar rol", ex.Message);
        }
    }

    // Legacy: Editing property and CancelEdit method not used by current UI
    // Code-behind calls grid.CancelEditRow directly; consider removal in future cleanup
}
