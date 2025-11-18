using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Auth.Web.Domain.Entities;
using Auth.Web.Services.Admin;
using Radzen;
using Radzen.Blazor;

namespace Auth.Web.Components.Admin;

public partial class AreasAdmin : ComponentBase
{
    private List<Area> areas = new();
    private string newArea = string.Empty;
    private RadzenDataGrid<Area> grid = default!;

    [Inject] private IAreaAdminService AreaAdmin { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        areas = await AreaAdmin.GetAreasAsync();
    }

    private async Task CreateArea()
    {
        var created = await AreaAdmin.CreateAsync(newArea);
        if (created is not null)
        {
            newArea = string.Empty;
            areas = await AreaAdmin.GetAreasAsync();
            await grid.Reload();
        }
    }

    private async Task OnRowUpdate(Area area)
    {
        if (await AreaAdmin.UpdateNameAsync(area.Id, area.Name))
        {
            areas = await AreaAdmin.GetAreasAsync();
        }
    }

    private async Task DeleteArea(int id)
    {
        if (await AreaAdmin.DeleteAsync(id))
        {
            areas = await AreaAdmin.GetAreasAsync();
            await grid.Reload();
        }
    }
}
