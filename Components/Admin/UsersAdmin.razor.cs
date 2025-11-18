using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Auth.Web.Services.Admin;

namespace Auth.Web.Components.Admin;

public partial class UsersAdmin : ComponentBase
{
    [Inject] private IUserAdminService UserAdmin { get; set; } = default!;

    private string search = string.Empty;
    private List<UserItem> users = new();
    private List<UserItem> filteredUsers = new();
    private UserItem? editUser;
    private List<string> allRoles = new();
    private HashSet<string> selectedRoles = new();
    private object roleEdit = new();

    protected override async Task OnInitializedAsync()
    {
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        (users, allRoles) = await UserAdmin.GetAsync();
        filteredUsers = users;
        StateHasChanged();
    }

    private void Filter()
    {
        filteredUsers = users
            .Where(u => string.IsNullOrWhiteSpace(search) || (u.UserName?.Contains(search, StringComparison.OrdinalIgnoreCase) == true) || (u.Email?.Contains(search, StringComparison.OrdinalIgnoreCase) == true))
            .ToList();
    }

    private void EditRoles(UserItem user)
    {
        editUser = user;
        selectedRoles = user.Roles.ToHashSet(System.StringComparer.OrdinalIgnoreCase);
        StateHasChanged();
    }

    private void ToggleRole(string role, bool selected)
    {
        if (selected) selectedRoles.Add(role); else selectedRoles.Remove(role);
    }

    private async Task SaveUserRoles()
    {
        if (editUser is null) return;
        await UserAdmin.UpdateRolesAsync(editUser.Id, selectedRoles);
        editUser = null;
        await ReloadAsync();
    }

    private void CancelEdit()
    {
        editUser = null;
    }
}
