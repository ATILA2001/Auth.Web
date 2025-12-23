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
    [Inject] private NotificationService NotificationService { get; set; } = null!;

    private UsersViewModel _vm = null!;
    private RadzenDataGrid<UserAdminDto> grid = null!;

    private readonly Dictionary<UserAdminDto, List<string>> _rolesBuffer = new();
    private readonly Dictionary<UserAdminDto, List<int>> _areasBuffer = new();

    private string search
    {
        get => _vm.Search;
        set => _vm.Search = value;
    }
    private List<UserAdminDto> filteredUsers => _vm.FilteredUsers;
    private List<RoleAdminDto> AllRoles => _vm.AllRoles;
    private List<AreaAdminDto> AllAreas => _vm.AllAreas;

    protected override void OnInitialized()
    {
        _vm = new UsersViewModel(UserService, RoleService, AreaService);
    }

    protected override async Task OnInitializedAsync()
    {
        await _vm.LoadAsync();
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

    private async Task EditRow(UserAdminDto user)
    {
        _vm.BeginEdit(user);
        await grid.EditRow(user);
    }

    private async Task OnRowUpdate(UserAdminDto user)
    {
        _vm.BeginEdit(user);
        _vm.SelectedRoles = _rolesBuffer.TryGetValue(user, out var roles) ? roles : new List<string>();
        _vm.SelectedAreaIds = _areasBuffer.TryGetValue(user, out var areas) ? areas : new List<int>();

        var result = await _vm.SaveAsync();
        NotifyUser(result);

        if (result.RequiresReload)
        {
            await _vm.LoadAsync();
            await grid.Reload();
        }
    }

    private void CancelEditRow(UserAdminDto user)
    {
        grid.CancelEditRow(user);
        _rolesBuffer.Remove(user);
        _areasBuffer.Remove(user);
    }

    private void Filter() => _vm.Filter();

    private void ClearFilters()
    {
        grid.Reset(true);
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
