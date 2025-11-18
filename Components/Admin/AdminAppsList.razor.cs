using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Auth.Web.Domain.Entities;
using Auth.Web.Services.Admin;

namespace Auth.Web.Components.Admin;

public partial class AdminAppsList : ComponentBase
{
    [Inject] private IRoutingAdminService RoutingSvc { get; set; } = default!;

    private List<Area> areas = new();
    private List<AreaRoute> rules = new();
    private bool loading = true;
    private string? error;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            (areas, rules) = await RoutingSvc.GetAsync();
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }
        finally
        {
            loading = false;
        }
    }
}
