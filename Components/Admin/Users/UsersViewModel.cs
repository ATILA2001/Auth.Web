using Auth.Web.Application.Admin.Dtos;
using Auth.Web.Services.Abstractions.Admin;

namespace Auth.Web.Components.Admin.Users;

public enum UsersVmOutcome
{
    Success,
    ValidationError,
    Error
}

public sealed class UsersVmResult
{
    public UsersVmOutcome Outcome { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public bool RequiresReload { get; init; }

    public static UsersVmResult Success(string title, string message, bool requiresReload = true) =>
        new() { Outcome = UsersVmOutcome.Success, Title = title, Message = message, RequiresReload = requiresReload };

    public static UsersVmResult ValidationFailed(string title, string message) =>
        new() { Outcome = UsersVmOutcome.ValidationError, Title = title, Message = message, RequiresReload = false };

    public static UsersVmResult Failed(string title, string message) =>
        new() { Outcome = UsersVmOutcome.Error, Title = title, Message = message, RequiresReload = false };
}

public sealed class UsersViewModel
{
    private readonly IAdminUserService _userService;
    private readonly IAdminRoleService _roleService;
    private readonly IAdminAreaService _areaService;
    private string _search = string.Empty;
    private List<string> _selectedRoles = new();
    private List<int> _selectedAreaIds = new();

    public UsersViewModel(IAdminUserService userService, IAdminRoleService roleService, IAdminAreaService areaService)
    {
        ArgumentNullException.ThrowIfNull(userService);
        ArgumentNullException.ThrowIfNull(roleService);
        ArgumentNullException.ThrowIfNull(areaService);
        _userService = userService;
        _roleService = roleService;
        _areaService = areaService;
    }

    public string Search
    {
        get => _search;
        set => _search = value?.Trim() ?? string.Empty;
    }

    public List<UserAdminDto> Users { get; private set; } = new();
    public List<UserAdminDto> FilteredUsers { get; private set; } = new();
    public UserAdminDto? SelectedUser { get; private set; }
    public List<RoleAdminDto> AllRoles { get; private set; } = new();
    public List<AreaAdminDto> AllAreas { get; private set; } = new();

    public List<string> SelectedRoles
    {
        get => _selectedRoles;
        set => _selectedRoles = value ?? new();
    }

    public List<int> SelectedAreaIds
    {
        get => _selectedAreaIds;
        set => _selectedAreaIds = value ?? new();
    }

    public async Task LoadAsync()
    {
        Users = (await _userService.GetUsersAsync()).ToList();
        AllRoles = (await _roleService.GetRolesAsync()).ToList();
        AllAreas = (await _areaService.GetAreasAsync()).ToList();
        Filter();
    }

    public void Filter()
    {
        var term = Search;
        FilteredUsers = Users
            .Where(u => string.IsNullOrWhiteSpace(term)
                || (u.UserName?.Contains(term, StringComparison.OrdinalIgnoreCase) == true)
                || (u.Email?.Contains(term, StringComparison.OrdinalIgnoreCase) == true))
            .ToList();
    }

    public void BeginEdit(UserAdminDto user)
    {
        ArgumentNullException.ThrowIfNull(user);
        var roles = user.Roles ?? Array.Empty<string>();
        var areas = user.Areas ?? Array.Empty<string>();
        var areaIds = user.AreaIds ?? Array.Empty<int>();

        SelectedUser = new UserAdminDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            Roles = roles.ToArray(),
            Areas = areas.ToArray(),
            AreaIds = areaIds.ToArray()
        };
        SelectedRoles = roles.ToList();
        SelectedAreaIds = areaIds.ToList();
    }

    public async Task<UsersVmResult> SaveAsync()
    {
        var validation = ValidateOnly();
        if (validation.Outcome != UsersVmOutcome.Success)
        {
            return validation;
        }

        if (SelectedUser is null)
        {
            return UsersVmResult.ValidationFailed("Validación", "No hay usuario seleccionado.");
        }

        try
        {
            var userName = string.IsNullOrWhiteSpace(SelectedUser.UserName) ? "(sin nombre)" : SelectedUser.UserName;
            await _userService.UpdateUserRolesAndAreasAsync(SelectedUser.Id, SelectedRoles, SelectedAreaIds);
            SelectedUser = null;
            return UsersVmResult.Success("Usuario actualizado", $"Se actualizaron roles/áreas de {userName}.");
        }
        catch (Exception ex)
        {
            return UsersVmResult.Failed("Error al actualizar usuario", ex.Message);
        }
    }

    public UsersVmResult ValidateOnly()
    {
        if (SelectedRoles.Count == 0)
        {
            return UsersVmResult.ValidationFailed("Validación", "Debe seleccionar al menos un rol.");
        }

        return UsersVmResult.Success("Válido", "", requiresReload: false);
    }

    public async Task<UsersVmResult> DeleteAsync(string userId)
    {
        try
        {
            await _userService.DeleteUserAsync(userId);
            // DELETE: reload required to remove row from grid
            return UsersVmResult.Success("Usuario eliminado", $"Id {userId} removido.", requiresReload: true);
        }
        catch (Exception ex)
        {
            return UsersVmResult.Failed("Error al eliminar usuario", ex.Message);
        }
    }

    public void CancelEdit()
    {
        SelectedUser = null;
    }
}
