using Microsoft.AspNetCore.Components;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Application.Admin.Dtos;
using Radzen;

namespace Auth.Web.Components.Admin;

public partial class UsersAdmin : ComponentBase
{
    [Inject] private IAdminUserService UserService { get; set; } = default!;
    [Inject] private IAdminRoleService RoleService { get; set; } = default!;
    [Inject] private IAdminAreaService AreaService { get; set; } = default!;
    [Inject] private NotificationService Notifications { get; set; } = default!;

    private string search = string.Empty;
    private List<UserAdminDto> users = new();
    private List<UserAdminDto> filteredUsers = new();

    private UserAdminDto? SelectedUser;
    private List<RoleAdminDto> AllRoles = new();
    private List<AreaAdminDto> AllAreas = new();
    private List<string> SelectedRoles = new();
    private List<int> SelectedAreaIds = new();

    protected override async Task OnInitializedAsync()
    {
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        users = (await UserService.GetUsersAsync()).ToList();
        filteredUsers = users;
        AllRoles = (await RoleService.GetRolesAsync()).ToList();
        AllAreas = (await AreaService.GetAreasAsync()).ToList();
        StateHasChanged();
    }

    private void Filter()
    {
        filteredUsers = users
            .Where(u => string.IsNullOrWhiteSpace(search)
                || (u.UserName?.Contains(search, StringComparison.OrdinalIgnoreCase) == true)
                || (u.Email?.Contains(search, StringComparison.OrdinalIgnoreCase) == true))
            .ToList();
    }

    private void BeginEdit(UserAdminDto user)
    {
        SelectedUser = user;
        SelectedRoles = user.Roles.ToList();
        SelectedAreaIds = user.AreaIds.ToList();
        StateHasChanged();
    }

    private async Task SaveUser()
    {
        if (SelectedUser is null) return;
        
        try
        {
            await UserService.UpdateUserRolesAndAreasAsync(SelectedUser.Id, SelectedRoles, SelectedAreaIds);
            Notifications.Notify(NotificationSeverity.Success, "Usuario actualizado", $"Se actualizaron roles/áreas de {SelectedUser.UserName}.");
            SelectedUser = null;
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            Notifications.Notify(NotificationSeverity.Error, "Error al actualizar usuario", ex.Message);
        }
    }

    private void CancelEdit() => SelectedUser = null;
}
