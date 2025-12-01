using Microsoft.AspNetCore.Components;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Application.Admin.Dtos;
using Radzen;
using Radzen.Blazor;

namespace Auth.Web.Components.Admin;

public partial class AreasAdmin : ComponentBase
{
    [Inject] private IAdminAreaService AdminAreaService { get; set; } = default!;
    [Inject] private NotificationService NotificationService { get; set; } = default!;

    private List<AreaAdminDto> areas = new();
    private string newArea = string.Empty;
    private RadzenDataGrid<AreaAdminDto> grid = default!;

    protected override async Task OnInitializedAsync()
    {
        areas = (await AdminAreaService.GetAreasAsync()).ToList();
    }

    private async Task CreateArea()
    {
        if (string.IsNullOrWhiteSpace(newArea))
        {
            NotificationService.Notify(NotificationSeverity.Warning, "Validación", "El nombre del área no puede estar vacío.");
            return;
        }

        try
        {
            var id = await AdminAreaService.CreateAreaAsync(newArea);
            if (id != 0)
            {
                NotificationService.Notify(NotificationSeverity.Success, "Área creada", $"Se creó '{newArea}' correctamente.");
                newArea = string.Empty;
                areas = (await AdminAreaService.GetAreasAsync()).ToList();
                await grid.Reload();
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Warning, "Sin cambios", "Nombre inválido o duplicado.");
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Error al crear área", ex.Message);
        }
    }

    private async Task OnRowUpdate(AreaAdminDto area)
    {
        if (string.IsNullOrWhiteSpace(area.Name))
        {
            NotificationService.Notify(NotificationSeverity.Warning, "Validación", "El nombre del área no puede estar vacío.");
            grid.CancelEditRow(area);
            return;
        }

        try
        {
            await AdminAreaService.UpdateAreaAsync(area.Id, area.Name);
            NotificationService.Notify(NotificationSeverity.Success, "Área actualizada", $"Se actualizó '{area.Name}'.");
            areas = (await AdminAreaService.GetAreasAsync()).ToList();
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Error al actualizar área", ex.Message);
        }
    }

    private async Task DeleteArea(int id)
    {
        try
        {
            await AdminAreaService.DeleteAreaAsync(id);
            NotificationService.Notify(NotificationSeverity.Success, "Área eliminada", $"Id {id} removido.");
            areas = (await AdminAreaService.GetAreasAsync()).ToList();
            await grid.Reload();
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Error al eliminar área", ex.Message);
        }
    }
}
