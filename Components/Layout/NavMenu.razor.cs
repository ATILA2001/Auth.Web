using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Auth.Web.Domain.Entities;
using Auth.Web.Application.Admin.Abstractions;

namespace Auth.Web.Components.Layout;

public partial class NavMenu : IDisposable
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IAdminRoutingService RoutingService { get; set; } = default!;
    
    private string? currentUrl;
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
        try
        {
            var routesDto = await RoutingService.GetRoutesAsync();
            
            rules = routesDto.Select(d => new AreaRoute
            {
                Id = d.Id,
                AreaId = d.AreaId,
                ClientId = d.ClientIdentifier,
                ReturnUrl = d.ReturnUrl,
                Priority = d.Priority,
                IsActive = d.IsActive
            }).ToList();
            
            areas = routesDto
                .Where(x => x.AreaName != null)
                .Select(r => new Area { Id = r.AreaId, Name = r.AreaName ?? $"Área {r.AreaId}" })
                .DistinctBy(a => a.Id)
                .ToList();
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
