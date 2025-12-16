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

    public static RolesVmResult Success(string title, string message, bool requiresReload = true) =>
        new() { Outcome = RolesVmOutcome.Success, Title = title, Message = message, RequiresReload = requiresReload };

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
    public string NewRoleName { get; set; } = string.Empty;

    public async Task LoadAsync()
    {
        Roles = (await _roleService.GetRolesAsync()).ToList();
    }

    public async Task<RolesVmResult> CreateAsync()
    {
        var name = NewRoleName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name))
        {
            return RolesVmResult.ValidationFailed("Validación", "El nombre del rol no puede estar vacío.");
        }

        try
        {
            var id = await _roleService.CreateRoleAsync(name);
            if (!string.IsNullOrWhiteSpace(id))
            {
                NewRoleName = string.Empty;
                return RolesVmResult.Success("Rol creado", $"Se creó '{name}'.");
            }
            else
            {
                return RolesVmResult.ValidationFailed("Sin cambios", "Nombre inválido o duplicado.");
            }
        }
        catch (Exception ex)
        {
            return RolesVmResult.Failed("Error al crear rol", ex.Message);
        }
    }

    public async Task<RolesVmResult> DeleteAsync(string roleId)
    {
        try
        {
            await _roleService.DeleteRoleAsync(roleId);
            return RolesVmResult.Success("Rol eliminado", $"Id {roleId} eliminado.");
        }
        catch (Exception ex)
        {
            return RolesVmResult.Failed("Error al eliminar rol", ex.Message);
        }
    }
}
