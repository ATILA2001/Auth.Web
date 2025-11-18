using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Auth.Web.Services.Admin;

namespace Auth.Web.Components.Admin;

public partial class RolesAdmin : ComponentBase
{
    private List<IdentityRole> roles = new();
    private string newRole = string.Empty;
    [Inject] private IRoleAdminService RoleAdmin { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        roles = await RoleAdmin.GetRolesAsync();
    }

    private async Task CreateRole()
    {
        if (await RoleAdmin.CreateRoleAsync(newRole))
        {
            newRole = string.Empty;
            roles = await RoleAdmin.GetRolesAsync();
        }
    }

    private async Task DeleteRole(string roleId)
    {
        if (await RoleAdmin.DeleteRoleAsync(roleId))
        {
            roles = await RoleAdmin.GetRolesAsync();
        }
    }
}
