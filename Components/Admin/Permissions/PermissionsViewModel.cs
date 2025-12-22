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

    public static PermissionsVmResult Success(string title, string message, bool requiresReload = true) =>
        new() { Outcome = PermissionsVmOutcome.Success, Title = title, Message = message, RequiresReload = requiresReload };

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

    public bool Editing { get; private set; }
    public string SelectedRoleId { get; set; } = string.Empty;
    public int SelectedPageId { get; set; }
    public int SelectedActionId { get; set; }
    public string? ValidationError { get; private set; }

    // Filtros
    public string? FilterRoleId { get; set; }
    public int? FilterPageId { get; set; }

    public async Task LoadAsync()
    {
        Roles = (await _roleService.GetRolesAsync()).ToList();
        Pages = (await _pageService.GetPagesAsync()).ToList();
        Actions = (await _actionService.GetActionsAsync()).ToList();

        await LoadPermissionsAsync();

        // Valores por defecto para el formulario
        if (Editing)
        {
            if (!Roles.Any(r => r.Id == SelectedRoleId))
                SelectedRoleId = Roles.FirstOrDefault()?.Id ?? string.Empty;

            if (!Pages.Any(p => p.Id == SelectedPageId))
                SelectedPageId = Pages.FirstOrDefault()?.Id ?? 0;

            if (!Actions.Any(a => a.Id == SelectedActionId))
                SelectedActionId = Actions.FirstOrDefault()?.Id ?? 0;
        }
    }

    public async Task LoadPermissionsAsync()
    {
        if (!string.IsNullOrEmpty(FilterRoleId))
        {
            Permissions = (await _permissionService.GetPermissionsByRoleAsync(FilterRoleId)).ToList();
        }
        else if (FilterPageId.HasValue && FilterPageId.Value > 0)
        {
            Permissions = (await _permissionService.GetPermissionsByPageAsync(FilterPageId.Value)).ToList();
        }
        else
        {
            Permissions = (await _permissionService.GetPermissionsAsync()).ToList();
        }
    }

    public void BeginCreate()
    {
        SelectedRoleId = FilterRoleId ?? Roles.FirstOrDefault()?.Id ?? string.Empty;
        SelectedPageId = FilterPageId ?? Pages.FirstOrDefault()?.Id ?? 0;
        SelectedActionId = Actions.FirstOrDefault()?.Id ?? 0;
        ValidationError = null;
        Editing = true;
    }

    public async Task<PermissionsVmResult> SaveAsync()
    {
        ValidationError = null;

        if (string.IsNullOrWhiteSpace(SelectedRoleId) || SelectedPageId <= 0 || SelectedActionId <= 0)
        {
            ValidationError = "Selecciona rol, página y acción";
            return PermissionsVmResult.ValidationFailed("Validación", ValidationError);
        }

        if (!Roles.Any(r => r.Id == SelectedRoleId))
        {
            ValidationError = "El rol seleccionado no existe";
            return PermissionsVmResult.ValidationFailed("Validación", ValidationError);
        }

        if (!Pages.Any(p => p.Id == SelectedPageId))
        {
            ValidationError = "La página seleccionada no existe";
            return PermissionsVmResult.ValidationFailed("Validación", ValidationError);
        }

        if (!Actions.Any(a => a.Id == SelectedActionId))
        {
            ValidationError = "La acción seleccionada no existe";
            return PermissionsVmResult.ValidationFailed("Validación", ValidationError);
        }

        try
        {
            var id = await _permissionService.CreatePermissionAsync(SelectedRoleId, SelectedPageId, SelectedActionId);
            Editing = false;
            ValidationError = null;
            return PermissionsVmResult.Success("Permiso creado", $"Permiso Id {id} asignado.");
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
            return PermissionsVmResult.Success("Permiso eliminado", $"Id {id} eliminado.");
        }
        catch (Exception ex)
        {
            return PermissionsVmResult.Failed("Error al eliminar permiso", ex.Message);
        }
    }

    public void CancelEdit()
    {
        Editing = false;
        ValidationError = null;
    }

    public async Task ApplyFilterAsync()
    {
        await LoadPermissionsAsync();
    }

    public async Task ClearFilterAsync()
    {
        FilterRoleId = null;
        FilterPageId = null;
        await LoadPermissionsAsync();
    }
}
