using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Auth.Web.Domain.Entities;
using Auth.Web.Application.Admin.Abstractions; // use new admin routing interface

namespace Auth.Web.Components.Layout;

public partial class NavMenu : IDisposable
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IAdminRoutingService RoutingService { get; set; } = default!;
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
            // New service returns DTOs; adapt by mapping if needed
            var routesDto = await RoutingService.GetRoutesAsync();
            // Map DTOs to existing AreaRoute shape for nav usage
            rules = routesDto.Select(d => new AreaRoute
            {
                Id = d.Id,
                AreaId = d.AreaId,
                ClientId = d.ClientIdentifier,
                ReturnUrl = d.ReturnUrl,
                Priority = d.Priority,
                IsActive = d.IsActive
            }).ToList();
            var areasDto = await RoutingService.GetRoutesAsync(); // reuse call for area names if needed
            areas = rules.Select(r => new Area { Id = r.AreaId, Name = routesDto.First(x => x.AreaId == r.AreaId).AreaName ?? $"Área {r.AreaId}" }).DistinctBy(a => a.Id).ToList();
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
