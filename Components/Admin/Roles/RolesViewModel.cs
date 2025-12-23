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
    public RoleAdminDto EditModel { get; private set; } = new() { Id = string.Empty, Name = string.Empty, UserCount = 0 };
    public string EditName { get; set; } = string.Empty;
    public bool Editing { get; internal set; }
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
        Editing = true;
    }

    public void BeginEdit(RoleAdminDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        EditModel = new RoleAdminDto { Id = dto.Id, Name = dto.Name, UserCount = dto.UserCount };
        EditName = dto.Name ?? string.Empty;
        ValidationError = null;
        Editing = true;
    }

    public async Task<RolesVmResult> SaveAsync()
    {
        ValidationError = null;
        var name = (EditName ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            ValidationError = "El nombre del rol no puede estar vacío.";
            return RolesVmResult.ValidationFailed("Validación", ValidationError);
        }

        var duplicate = Roles.Any(r => r.Id != EditModel.Id && string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase));
        if (duplicate)
        {
            ValidationError = "Ya existe un rol con ese nombre.";
            return RolesVmResult.ValidationFailed("Validación", ValidationError);
        }

        try
        {
            if (string.IsNullOrWhiteSpace(EditModel.Id))
            {
                var id = await _roleService.CreateRoleAsync(name);
                Editing = false;
                ValidationError = null;
                return RolesVmResult.Success("Rol creado", $"Se creó '{name}'.");
            }

            await _roleService.RenameRoleAsync(EditModel.Id, name);
            Editing = false;
            ValidationError = null;
            return RolesVmResult.Success("Rol actualizado", $"Se actualizó '{name}'.");
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
            return RolesVmResult.Success("Rol eliminado", $"Id {roleId} eliminado.");
        }
        catch (Exception ex)
        {
            return RolesVmResult.Failed("Error al eliminar rol", ex.Message);
        }
    }

    public void CancelEdit()
    {
        Editing = false;
        ValidationError = null;
        EditName = string.Empty;
        EditModel = new RoleAdminDto { Id = string.Empty, Name = string.Empty, UserCount = 0 };
    }
}
