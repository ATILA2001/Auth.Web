using Microsoft.AspNetCore.Components;
using Auth.Web.Application.Admin.Abstractions;
using Auth.Web.Application.Admin.Dtos;
using Radzen;

namespace Auth.Web.Components.Admin;

public partial class ClientsAdmin : ComponentBase
{
    [Inject] private IAdminClientService ClientService { get; set; } = default!;
    [Inject] private NotificationService NotificationService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private List<ApplicationClientAdminDto> clients = new();
    private bool editing;
    private ApplicationClientAdminDto editModel = new();
    private string allowedUrlsText = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        clients = (await ClientService.GetClientsAsync()).ToList();
    }

    private void BeginCreate()
    {
        editModel = new ApplicationClientAdminDto { Id = 0, ClientId = string.Empty, Audience = string.Empty, AllowedReturnUrls = Array.Empty<string>() };
        allowedUrlsText = string.Empty;
        editing = true;
    }

    private void BeginEdit(ApplicationClientAdminDto dto)
    {
        editModel = dto;
        allowedUrlsText = string.Join("\n", dto.AllowedReturnUrls);
        editing = true;
    }

    private async Task SaveClient()
    {
        var urls = allowedUrlsText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        try
        {
            if (editModel.Id == 0)
            {
                var id = await ClientService.CreateClientAsync(editModel.ClientId, editModel.Audience, urls);
                NotificationService.Notify(NotificationSeverity.Success, "Cliente creado", $"Se creó '{editModel.ClientId}' (Id {id}).");
            }
            else
            {
                await ClientService.UpdateClientAsync(editModel.Id, editModel.ClientId, editModel.Audience, urls);
                NotificationService.Notify(NotificationSeverity.Success, "Cliente actualizado", $"Se actualizó '{editModel.ClientId}'.");
            }
            editing = false;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Error al guardar cliente", ex.Message);
        }
    }

    private async Task DeleteClient(int id)
    {
        try
        {
            await ClientService.DeleteClientAsync(id);
            NotificationService.Notify(NotificationSeverity.Success, "Cliente eliminado", $"Id {id} eliminado.");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Error al eliminar cliente", ex.Message);
        }
    }

    private void CancelEdit()
    {
        editing = false;
    }
}
