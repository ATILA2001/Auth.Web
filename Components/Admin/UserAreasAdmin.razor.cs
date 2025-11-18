using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Auth.Web.Domain.Entities;
using Auth.Web.Services.Admin;

namespace Auth.Web.Components.Admin;

public partial class UserAreasAdmin : ComponentBase
{
    private List<Area> areas = new();
    private List<UserArea> userAreas = new();
    private List<UserListItem> users = new();
    private string selectedUserId = string.Empty;
    private int selectedAreaId = 0;
    [Inject] private IUserAreaAdminService UserAreaAdmin { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        var data = await UserAreaAdmin.GetAsync();
        areas = data.Areas;
        userAreas = data.UserAreas;
        users = data.Users;
    }

    private async Task Assign()
    {
        if (string.IsNullOrWhiteSpace(selectedUserId) || selectedAreaId == 0) return;

        if (await UserAreaAdmin.AssignAsync(selectedUserId, selectedAreaId))
        {
            await ReloadAsync();
        }
    }

    private async Task Remove(int userAreaId)
    {
        if (await UserAreaAdmin.RemoveAsync(userAreaId))
        {
            await ReloadAsync();
        }
    }
}
