using Microsoft.AspNetCore.Components;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Application.Admin.Dtos;
using Radzen;

namespace Auth.Web.Components.Admin.Permissions;

public partial class Permissions : ComponentBase
{
    [Inject] private IAdminAreaPagePermissionService PermissionService { get; set; } = null!;
    [Inject] private IAdminAreaService AreaService { get; set; } = null!;
    [Inject] private IAdminPageService PageService { get; set; } = null!;
    [Inject] private IAdminActionPermissionService ActionService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;

    private PermissionsViewModel _vm = null!;
    private int _selectedAreaId;
    private string _selectedAreaName = string.Empty;
    private bool _selectedAreaHasClient;

    private bool IsLoading { get; set; }
    private bool IsLoadingMatrix { get; set; }
    private bool IsSaving { get; set; }

    protected override void OnInitialized()
    {
        _vm = new PermissionsViewModel(PermissionService, AreaService, PageService, ActionService);
    }

    protected override async Task OnInitializedAsync()
    {
        IsLoading = true;
        try
        {
            await _vm.LoadAsync();
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Error al cargar", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task OnAreaSelected(object value)
    {
        if (value is not int areaId) return;
        _selectedAreaId = areaId;
        var area = _vm.Areas.FirstOrDefault(a => a.Id == areaId);
        _selectedAreaName = area?.Name ?? string.Empty;
        _selectedAreaHasClient = area?.ClientId.HasValue == true;
        await LoadMatrixAsync();
    }

    private async Task LoadMatrixAsync()
    {
        if (_selectedAreaId == 0) return;
        if (!_selectedAreaHasClient) { StateHasChanged(); return; }

        IsLoadingMatrix = true;
        StateHasChanged();
        try
        {
            await _vm.LoadMatrixForAreaAsync(_selectedAreaId);
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Error al cargar permisos", ex.Message);
        }
        finally
        {
            IsLoadingMatrix = false;
        }
    }

    private async Task TogglePermissionAsync(PermissionMatrixRow row, int actionId, bool enabled)
    {
        if (IsSaving) return;

        IsSaving = true;
        try
        {
            if (enabled)
            {
                var id = await PermissionService.CreatePermissionAsync(_selectedAreaId, row.PageId, actionId);
                row.ActionMap[actionId] = id;
            }
            else
            {
                var permissionId = row.GetPermissionId(actionId);
                if (permissionId.HasValue)
                {
                    await PermissionService.DeletePermissionAsync(permissionId.Value);
                    row.ActionMap[actionId] = null;
                }
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Error al guardar permiso", ex.Message);
            await LoadMatrixAsync();
        }
        finally
        {
            IsSaving = false;
            StateHasChanged();
        }
    }

    private async Task ToggleAllAsync(PermissionMatrixRow row, bool enabled)
    {
        if (IsSaving) return;

        IsSaving = true;
        try
        {
            foreach (var action in _vm.Actions)
            {
                if (enabled)
                {
                    var id = await PermissionService.CreatePermissionAsync(_selectedAreaId, row.PageId, action.Id);
                    row.ActionMap[action.Id] = id;
                }
                else
                {
                    var permissionId = row.GetPermissionId(action.Id);
                    if (permissionId.HasValue)
                    {
                        await PermissionService.DeletePermissionAsync(permissionId.Value);
                        row.ActionMap[action.Id] = null;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Error al guardar permisos", ex.Message);
            await LoadMatrixAsync();
        }
        finally
        {
            IsSaving = false;
            StateHasChanged();
        }
    }

    private static string TranslateAction(string name) => name.ToLowerInvariant() switch
    {
        "read"   => "Ver",
        "create" => "Agregar",
        "edit"   => "Modificar",
        "delete" => "Eliminar",
        _        => name
    };
}