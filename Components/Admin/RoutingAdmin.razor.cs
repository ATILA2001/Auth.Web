using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Auth.Web.Domain.Entities;
using Auth.Web.Services.Admin;
using Radzen.Blazor;

namespace Auth.Web.Components.Admin;

public partial class RoutingAdmin : ComponentBase
{
    private List<Area> areas = new();
    private List<AreaRoute> rules = new();
    private AreaRoute newRule = new() { Priority = 1, IsActive = true };
    private RadzenDataGrid<AreaRoute> grid = default!;

    [Inject] private IRoutingAdminService RoutingSvc { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        (areas, rules) = await RoutingSvc.GetAsync();
    }

    private async Task AddRule()
    {
        var created = await RoutingSvc.CreateAsync(newRule);
        if (created is not null)
        {
            newRule = new AreaRoute { Priority = 1, IsActive = true };
            await ReloadAsync();
            await grid.Reload();
        }
    }

    private async Task Save(AreaRoute r)
    {
        if (await RoutingSvc.UpdateAsync(r))
        {
            await ReloadAsync();
        }
    }

    private async Task Delete(int id)
    {
        if (await RoutingSvc.DeleteAsync(id))
        {
            await ReloadAsync();
            await grid.Reload();
        }
    }
}
