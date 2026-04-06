using Microsoft.AspNetCore.Components;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Application.Admin.Dtos;
using Radzen;
using Radzen.Blazor;

namespace Auth.Web.Components.Admin.Users;

public partial class Users : ComponentBase
{
    [Inject] private IAdminUserService UserService { get; set; } = null!;
    [Inject] private IAdminRoleService RoleService { get; set; } = null!;
    [Inject] private IAdminAreaService AreaService { get; set; } = null!;
    [Inject] private IAdminUserPageOverrideService OverrideService { get; set; } = null!;
    [Inject] private IAdminPageService PageService { get; set; } = null!;
    [Inject] private IAdminActionPermissionService ActionService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;

    private UsersViewModel _vm = null!;
    private RadzenDataGrid<UserAdminDto> grid = null!;

    private readonly Dictionary<UserAdminDto, List<string>> _rolesBuffer = new();
    private readonly Dictionary<UserAdminDto, List<int>> _areasBuffer = new();

    private string _selectedUserId = string.Empty;
    private string _selectedUserName = string.Empty;

    private List<RoleAdminDto> AllRoles => _vm.AllRoles;
    private List<AreaAdminDto> AllAreas => _vm.AllAreas;

    private bool IsLoading { get; set; }
    private bool IsSaving { get; set; }

    protected override void OnInitialized()
    {
        _vm = new UsersViewModel(UserService, RoleService, AreaService);
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync(reloadGrid: false);
    }

    private async Task LoadAsync(bool reloadGrid)
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        try
        {
            await _vm.LoadAsync();
            _rolesBuffer.Clear();
            _areasBuffer.Clear();
            if (reloadGrid && grid is not null)
            {
                await grid.Reload();
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "No se pudieron cargar los usuarios.", ex.Message);
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private IEnumerable<string> GetRolesBuffer(UserAdminDto user)
    {
        if (!_rolesBuffer.TryGetValue(user, out var value))
        {
            value = (user.Roles ?? Array.Empty<string>()).ToList();
            _rolesBuffer[user] = value;
        }
        return value;
    }

    private void SetRolesBuffer(UserAdminDto user, List<string> roles)
    {
        _rolesBuffer[user] = roles;
    }

    private IEnumerable<int> GetAreasBuffer(UserAdminDto user)
    {
        if (!_areasBuffer.TryGetValue(user, out var value))
        {
            value = (user.AreaIds ?? Array.Empty<int>()).ToList();
            _areasBuffer[user] = value;
        }
        return value;
    }

    private void SetAreasBuffer(UserAdminDto user, List<int> areas)
    {
        _areasBuffer[user] = areas;
    }

    private async Task OpenUserOverridesDialog(UserAdminDto user)
    {
        _selectedUserId = user.Id;
        _selectedUserName = string.IsNullOrWhiteSpace(user.FullName) ? user.UserName : user.FullName;

        var parameters = new Dictionary<string, object?>
        {
            ["UserId"] = _selectedUserId,
            ["UserName"] = _selectedUserName,
            ["OverrideService"] = OverrideService,
            ["PageService"] = PageService,
            ["ActionService"] = ActionService,
            ["NotificationService"] = NotificationService
        };

        var options = new DialogOptions
        {
            Width = "80%",
            Height = "80%",
            CloseDialogOnEsc = true
        };

        await DialogService.OpenAsync<UserOverridesPanel>($"Overrides de permisos — {_selectedUserName}", parameters, options);
        _selectedUserId = string.Empty;
        _selectedUserName = string.Empty;
    }

    private async Task EditRow(UserAdminDto user)
    {
        if (IsLoading || IsSaving)
        {
            return;
        }

        _vm.BeginEdit(user);
        await grid.EditRow(user);
    }

    private async Task OnRowUpdate(UserAdminDto user)
    {
        if (IsSaving)
        {
            return;
        }

        IsSaving = true;
        try
        {
            _vm.BeginEdit(user);
            _vm.SelectedRoles = _rolesBuffer.TryGetValue(user, out var roles) ? roles : new List<string>();
            _vm.SelectedAreaIds = _areasBuffer.TryGetValue(user, out var areas) ? areas : new List<int>();

            var result = await _vm.SaveAsync();
            NotifyUser(result);

            if (result.RequiresReload)
            {
                await LoadAsync(reloadGrid: true);
            }
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void CancelEditRow(UserAdminDto user)
    {
        if (IsSaving)
        {
            return;
        }

        grid.CancelEditRow(user);
        _rolesBuffer.Remove(user);
        _areasBuffer.Remove(user);
    }

    private async Task ValidateAndSave(UserAdminDto user)
    {
        if (IsLoading || IsSaving)
        {
            return;
        }

        _vm.BeginEdit(user);
        _vm.SelectedRoles = GetRolesBuffer(user).ToList();
        _vm.SelectedAreaIds = GetAreasBuffer(user).ToList();
        var validationResult = _vm.ValidateOnly();

        if (validationResult.Outcome != UsersVmOutcome.Success)
        {
            NotifyUser(validationResult);
            return;
        }

        await grid.UpdateRow(user);
    }

    private void ClearFilters()
    {
        if (IsLoading || IsSaving)
        {
            return;
        }

        grid.Reset(true);
    }

    private async Task DeleteUser(string id)
    {
        if (IsSaving)
        {
            return;
        }

        var confirm = await DialogService.Confirm("Eliminar el usuario?", "Confirmar", new ConfirmOptions { OkButtonText = "Eliminar", CancelButtonText = "Cancelar", Icon = "warning" });
        if (confirm != true)
        {
            return;
        }

        IsSaving = true;
        try
        {
            var result = await _vm.DeleteAsync(id);
            NotifyUser(result);

            // Explicit contract: DELETE success requires reload to remove row from grid
            if (result.RequiresReload)
            {
                await LoadAsync(reloadGrid: true);
            }
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void NotifyUser(UsersVmResult result)
    {
        var severity = result.Outcome switch
        {
            UsersVmOutcome.Success => NotificationSeverity.Success,
            UsersVmOutcome.ValidationError => NotificationSeverity.Warning,
            UsersVmOutcome.Error => NotificationSeverity.Error,
            _ => NotificationSeverity.Info
        };

        NotificationService.Notify(severity, result.Title, result.Message);
    }
}
