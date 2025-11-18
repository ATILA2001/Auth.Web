using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Auth.Web.Domain.Entities;
using Auth.Web.Services.Admin;

namespace Auth.Web.Components.Layout;

public partial class NavMenu : IDisposable
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IRoutingAdminService RoutingSvc { get; set; } = default!;
    private string? currentUrl;

    // Datos aplicativos
    private List<Area> areas = new();
    private List<AreaRoute> rules = new();
    private bool appsLoading = true;
    private string? appsError;
    private int activeCount => rules.Count(r => r.IsActive);

    protected override void OnInitialized()
    {
        currentUrl = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
        NavigationManager.LocationChanged += OnLocationChanged;
    }

    protected override async Task OnInitializedAsync()
    {
        // Carga de aplicativos
        try
        {
            (areas, rules) = await RoutingSvc.GetAsync();
        }
        catch (Exception ex)
        {
            appsError = ex.Message;
        }
        finally
        {
            appsLoading = false;
            StateHasChanged();
        }
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        currentUrl = NavigationManager.ToBaseRelativePath(e.Location);
        StateHasChanged();
    }

    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
    }
}
