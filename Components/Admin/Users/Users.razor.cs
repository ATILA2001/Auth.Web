using Microsoft.AspNetCore.Components;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Application.Admin.Dtos;
using Radzen;

namespace Auth.Web.Components.Admin.Users;

public partial class Users : ComponentBase
{
    [Inject] private IAdminUserService UserService { get; set; } = null!;
    [Inject] private IAdminRoleService RoleService { get; set; } = null!;
    [Inject] private IAdminAreaService AreaService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;

    private UsersViewModel _vm = null!;

    // Expose VM state with same names for Razor binding compatibility
    private string search
    {
        get => _vm.Search;
        set => _vm.Search = value;
    }
    private List<UserAdminDto> filteredUsers => _vm.FilteredUsers;
    private UserAdminDto? SelectedUser => _vm.SelectedUser;
    private List<RoleAdminDto> AllRoles => _vm.AllRoles;
    private List<AreaAdminDto> AllAreas => _vm.AllAreas;
    private List<string> SelectedRoles
    {
        get => _vm.SelectedRoles;
        set => _vm.SelectedRoles = value;
    }
    private List<int> SelectedAreaIds
    {
        get => _vm.SelectedAreaIds;
        set => _vm.SelectedAreaIds = value;
    }

    protected override void OnInitialized()
    {
        _vm = new UsersViewModel(UserService, RoleService, AreaService);
    }

    protected override async Task OnInitializedAsync()
    {
        await _vm.LoadAsync();
    }

    private void Filter() => _vm.Filter();

    private void BeginEdit(UserAdminDto user) => _vm.BeginEdit(user);

    private async Task SaveUser()
    {
        var result = await _vm.SaveAsync();
        NotifyUser(result);

        if (result.RequiresReload)
        {
            await _vm.LoadAsync();
        }
    }

    private void CancelEdit() => _vm.CancelEdit();

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
