using Auth.Web.Application.Admin.Dtos;
using Auth.Web.Services.Abstractions.Admin;

namespace Auth.Web.Components.Admin.Permissions;

public enum PermissionsVmOutcome
{
    Success,
    ValidationError,
    Error
}

public sealed class PermissionsVmResult
{
    public PermissionsVmOutcome Outcome { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public bool RequiresReload { get; init; }
    public int? CreatedId { get; init; }

    public static PermissionsVmResult Success(string title, string message, bool requiresReload = true) =>
        new() { Outcome = PermissionsVmOutcome.Success, Title = title, Message = message, RequiresReload = requiresReload };

    public static PermissionsVmResult CreateSuccess(string title, string message, int createdId) =>
        new() { Outcome = PermissionsVmOutcome.Success, Title = title, Message = message, RequiresReload = false, CreatedId = createdId };

    public static PermissionsVmResult ValidationFailed(string title, string message) =>
        new() { Outcome = PermissionsVmOutcome.ValidationError, Title = title, Message = message, RequiresReload = false };

    public static PermissionsVmResult Failed(string title, string message) =>
        new() { Outcome = PermissionsVmOutcome.Error, Title = title, Message = message, RequiresReload = false };
}

public sealed class PermissionsViewModel
{
    private readonly IAdminRolePagePermissionService _permissionService;
    private readonly IAdminRoleService _roleService;
    private readonly IAdminPageService _pageService;
    private readonly IAdminActionPermissionService _actionService;

    public PermissionsViewModel(
        IAdminRolePagePermissionService permissionService,
        IAdminRoleService roleService,
        IAdminPageService pageService,
        IAdminActionPermissionService actionService)
    {
        ArgumentNullException.ThrowIfNull(permissionService);
        ArgumentNullException.ThrowIfNull(roleService);
        ArgumentNullException.ThrowIfNull(pageService);
        ArgumentNullException.ThrowIfNull(actionService);

        _permissionService = permissionService;
        _roleService = roleService;
        _pageService = pageService;
        _actionService = actionService;
    }

    public List<RolePagePermissionAdminDto> Permissions { get; private set; } = new();
    public List<RoleAdminDto> Roles { get; private set; } = new();
    public List<PageAdminDto> Pages { get; private set; } = new();
    public List<ActionPermissionAdminDto> Actions { get; private set; } = new();
    public string SelectedRoleId { get; set; } = string.Empty;
    public int SelectedPageId { get; set; }
    public int SelectedActionId { get; set; }
    public string? ValidationError { get; private set; }

    public async Task LoadAsync()
    {
        Roles = (await _roleService.GetRolesAsync()).ToList();
        Pages = (await _pageService.GetPagesAsync()).ToList();
        Actions = (await _actionService.GetActionsAsync()).ToList();
        Permissions = (await _permissionService.GetPermissionsAsync()).ToList();
    }

    public void BeginCreate()
    {
        SelectedRoleId = Roles.FirstOrDefault()?.Id ?? string.Empty;
        SelectedPageId = Pages.FirstOrDefault()?.Id ?? 0;
        SelectedActionId = Actions.FirstOrDefault()?.Id ?? 0;
        ValidationError = null;
    }

    public PermissionsVmResult ValidateOnly(string roleId, int pageId, int actionId)
    {
        ValidationError = null;

        if (string.IsNullOrWhiteSpace(roleId))
        {
            ValidationError = "Debe seleccionar un rol.";
            return PermissionsVmResult.ValidationFailed("Validación", ValidationError);
        }

        if (pageId <= 0)
        {
            ValidationError = "Debe seleccionar una página.";
            return PermissionsVmResult.ValidationFailed("Validación", ValidationError);
        }

        if (actionId <= 0)
        {
            ValidationError = "Debe seleccionar una acción.";
            return PermissionsVmResult.ValidationFailed("Validación", ValidationError);
        }

        if (!Roles.Any(r => r.Id == roleId))
        {
            ValidationError = "El rol seleccionado no existe.";
            return PermissionsVmResult.ValidationFailed("Validación", ValidationError);
        }

        if (!Pages.Any(p => p.Id == pageId))
        {
            ValidationError = "La página seleccionada no existe.";
            return PermissionsVmResult.ValidationFailed("Validación", ValidationError);
        }

        if (!Actions.Any(a => a.Id == actionId))
        {
            ValidationError = "La acción seleccionada no existe.";
            return PermissionsVmResult.ValidationFailed("Validación", ValidationError);
        }

        // Check for duplicate
        var duplicate = Permissions.Any(p => p.RoleId == roleId && p.PageId == pageId && p.ActionPermissionId == actionId);
        if (duplicate)
        {
            ValidationError = "Ya existe este permiso.";
            return PermissionsVmResult.ValidationFailed("Validación", ValidationError);
        }

        return PermissionsVmResult.Success("Vólido", "", requiresReload: false);
    }

    public async Task<PermissionsVmResult> SaveAsync()
    {
        // Reuse validation logic to avoid duplication and rule drift
        var validationResult = ValidateOnly(SelectedRoleId, SelectedPageId, SelectedActionId);
        if (validationResult.Outcome != PermissionsVmOutcome.Success)
        {
            return validationResult;
        }

        try
        {
            // CREATE only (no UPDATE for permissions - delete and recreate instead)
            var id = await _permissionService.CreatePermissionAsync(SelectedRoleId, SelectedPageId, SelectedActionId);
            if (id != 0)
            {
                ValidationError = null;
                return PermissionsVmResult.CreateSuccess("Permiso creado", $"Permiso asignado.", id);
            }
            ValidationError = "No se pudo crear el permiso.";
            return PermissionsVmResult.ValidationFailed("Sin cambios", ValidationError);
        }
        catch (Exception ex)
        {
            ValidationError = ex.Message;
            return PermissionsVmResult.Failed("Error al crear permiso", ex.Message);
        }
    }

    public async Task<PermissionsVmResult> DeleteAsync(int id)
    {
        try
        {
            await _permissionService.DeletePermissionAsync(id);
            // DELETE: reload required to remove row from grid
            return PermissionsVmResult.Success("Permiso eliminado", $"Id {id} removido.", requiresReload: true);
        }
        catch (Exception ex)
        {
            return PermissionsVmResult.Failed("Error al eliminar permiso", ex.Message);
        }
    }

    // Legacy: Editing property and CancelEdit method not used by current UI
    // Code-behind calls grid.CancelEditRow directly; consider removal in future cleanup
}
