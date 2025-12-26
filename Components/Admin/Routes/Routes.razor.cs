using Microsoft.AspNetCore.Components;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Application.Admin.Dtos;
using Radzen;
using Radzen.Blazor;

namespace Auth.Web.Components.Admin.Routes;

public partial class Routes : ComponentBase
{
    [Inject] private IAdminRoutingService RoutingService { get; set; } = null!;
    [Inject] private IAdminAreaService AreaService { get; set; } = null!;
    [Inject] private IAdminClientService ClientService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;

    private RoutesViewModel _vm = null!;
    private RadzenDataGrid<AreaRouteAdminDto> grid = null!;

    private readonly Dictionary<AreaRouteAdminDto, int> _areaBuffer = new();
    private readonly Dictionary<AreaRouteAdminDto, int> _clientBuffer = new();
    private readonly Dictionary<AreaRouteAdminDto, string> _urlBuffer = new();
    private readonly Dictionary<AreaRouteAdminDto, int> _priorityBuffer = new();
    private readonly Dictionary<AreaRouteAdminDto, bool> _activeBuffer = new();

    private List<AreaRouteAdminDto> routes => _vm.Routes;
    private List<AreaAdminDto> areas => _vm.Areas;
    private List<ApplicationClientAdminDto> clients => _vm.Clients;

    private bool IsLoading { get; set; }
    private bool IsSaving { get; set; }

    protected override void OnInitialized()
    {
        _vm = new RoutesViewModel(RoutingService, AreaService, ClientService);
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync(reloadGrid: false);
    }

    private async Task LoadAsync(bool reloadGrid)
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        try
        {
            await _vm.LoadAsync();
            _areaBuffer.Clear();
            _clientBuffer.Clear();
            _urlBuffer.Clear();
            _priorityBuffer.Clear();
            _activeBuffer.Clear();
            if (reloadGrid && grid is not null)
            {
                await grid.Reload();
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "No se pudieron cargar las rutas.", ex.Message);
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private int GetAreaBuffer(AreaRouteAdminDto route)
    {
        if (!_areaBuffer.TryGetValue(route, out var value))
        {
            value = route.AreaId;
            _areaBuffer[route] = value;
        }
        return value;
    }

    private void SetAreaBuffer(AreaRouteAdminDto route, int value) => _areaBuffer[route] = value;

    private int GetClientBuffer(AreaRouteAdminDto route)
    {
        if (!_clientBuffer.TryGetValue(route, out var value))
        {
            value = route.ClientId;
            _clientBuffer[route] = value;
        }
        return value;
    }

    private void SetClientBuffer(AreaRouteAdminDto route, int value) => _clientBuffer[route] = value;

    private string GetReturnUrlBuffer(AreaRouteAdminDto route)
    {
        if (!_urlBuffer.TryGetValue(route, out var value))
        {
            value = route.ReturnUrl;
            _urlBuffer[route] = value;
        }
        return value;
    }

    private void SetReturnUrlBuffer(AreaRouteAdminDto route, string value) => _urlBuffer[route] = value;

    private int GetPriorityBuffer(AreaRouteAdminDto route)
    {
        if (!_priorityBuffer.TryGetValue(route, out var value))
        {
            value = route.Priority;
            _priorityBuffer[route] = value;
        }
        return value;
    }

    private void SetPriorityBuffer(AreaRouteAdminDto route, int value) => _priorityBuffer[route] = value;

    private bool GetActiveBuffer(AreaRouteAdminDto route)
    {
        if (!_activeBuffer.TryGetValue(route, out var value))
        {
            value = route.IsActive;
            _activeBuffer[route] = value;
        }
        return value;
    }

    private void SetActiveBuffer(AreaRouteAdminDto route, bool value) => _activeBuffer[route] = value;

    private async Task BeginCreate()
    {
        if (IsLoading || IsSaving)
        {
            return;
        }

        _vm.BeginCreate();
        var newRoute = new AreaRouteAdminDto
        {
            Id = 0,
            AreaId = areas.FirstOrDefault()?.Id ?? 0,
            ClientId = clients.FirstOrDefault()?.Id ?? 0,
            ReturnUrl = string.Empty,
            Priority = 1,
            IsActive = true,
            AreaName = areas.FirstOrDefault(a => a.Id == _vm.SelectedAreaId)?.Name,
            ApplicationName = clients.FirstOrDefault(c => c.Id == _vm.SelectedClientId)?.Audience
        };
        routes.Insert(0, newRoute);
        await grid.InsertRow(newRoute);
    }

    private async Task OnRowCreate(AreaRouteAdminDto route)
    {
        if (IsSaving)
        {
            return;
        }

        IsSaving = true;
        try
        {
            _vm.BeginCreate();
            _vm.SelectedAreaId = GetAreaBuffer(route);
            _vm.SelectedClientId = GetClientBuffer(route);
            _vm.EditReturnUrl = GetReturnUrlBuffer(route);
            _vm.EditPriority = GetPriorityBuffer(route);
            _vm.EditIsActive = GetActiveBuffer(route);

            var result = await _vm.SaveAsync();
            NotifyUser(result);

            if (result.RequiresReload)
            {
                await LoadAsync(reloadGrid: true);
            }

            if (result.Outcome == RoutesVmOutcome.ValidationError)
            {
                routes.Remove(route);
                await grid.Reload();
            }
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task EditRow(AreaRouteAdminDto route)
    {
        if (IsLoading || IsSaving)
        {
            return;
        }

        await grid.EditRow(route);
    }

    private async Task OnRowUpdate(AreaRouteAdminDto route)
    {
        if (IsSaving)
        {
            return;
        }

        IsSaving = true;
        try
        {
            _vm.BeginEdit(route);
            _vm.SelectedAreaId = GetAreaBuffer(route);
            _vm.SelectedClientId = GetClientBuffer(route);
            _vm.EditReturnUrl = GetReturnUrlBuffer(route);
            _vm.EditPriority = GetPriorityBuffer(route);
            _vm.EditIsActive = GetActiveBuffer(route);

            var result = await _vm.SaveAsync();
            NotifyUser(result);

            if (result.RequiresReload)
            {
                await LoadAsync(reloadGrid: true);
            }
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task DeleteRoute(int id)
    {
        if (IsSaving)
        {
            return;
        }

        var confirm = await DialogService.Confirm("¿Eliminar la ruta?", "Confirmar", new ConfirmOptions { OkButtonText = "Eliminar", CancelButtonText = "Cancelar", Icon = "warning" });
        if (confirm != true)
        {
            return;
        }

        IsSaving = true;
        try
        {
            var result = await _vm.DeleteAsync(id);
            NotifyUser(result);

            if (result.RequiresReload)
            {
                await LoadAsync(reloadGrid: true);
            }
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void CancelEditRow(AreaRouteAdminDto route)
    {
        if (IsSaving)
        {
            return;
        }

        grid.CancelEditRow(route);
        _areaBuffer.Remove(route);
        _clientBuffer.Remove(route);
        _urlBuffer.Remove(route);
        _priorityBuffer.Remove(route);
        _activeBuffer.Remove(route);
        if (route.Id == 0)
        {
            routes.Remove(route);
        }
    }

    private void ClearFilters()
    {
        if (IsLoading || IsSaving)
        {
            return;
        }

        grid.Reset(true);
    }

    private void NotifyUser(RoutesVmResult result)
    {
        var severity = result.Outcome switch
        {
            RoutesVmOutcome.Success => NotificationSeverity.Success,
            RoutesVmOutcome.ValidationError => NotificationSeverity.Warning,
            RoutesVmOutcome.Error => NotificationSeverity.Error,
            _ => NotificationSeverity.Info
        };

        NotificationService.Notify(severity, result.Title, result.Message);
    }
}
