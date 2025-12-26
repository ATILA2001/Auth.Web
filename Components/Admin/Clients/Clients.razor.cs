using Microsoft.AspNetCore.Components;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Application.Admin.Dtos;
using Radzen;
using Radzen.Blazor;

namespace Auth.Web.Components.Admin.Clients;

public partial class Clients : ComponentBase
{
    [Inject] private IAdminClientService ClientService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;

    private ClientsViewModel _vm = null!;
    private RadzenDataGrid<ApplicationClientAdminDto> grid = null!;
    private readonly Dictionary<ApplicationClientAdminDto, string> _urlsBuffer = new();

    private List<ApplicationClientAdminDto> clients => _vm.Clients;
    private string allowedUrlsText
    {
        get => _vm.AllowedUrlsText;
        set => _vm.AllowedUrlsText = value;
    }

    protected override void OnInitialized()
    {
        _vm = new ClientsViewModel(ClientService);
    }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await _vm.LoadAsync();
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "No se pudieron cargar los clientes.", ex.Message);
        }
    }

    private string GetUrlsBuffer(ApplicationClientAdminDto client)
    {
        if (!_urlsBuffer.TryGetValue(client, out var value))
        {
            value = string.Join("\n", client.AllowedReturnUrls);
            _urlsBuffer[client] = value;
        }
        return value;
    }

    private void SetUrlsBuffer(ApplicationClientAdminDto client, string value)
    {
        _urlsBuffer[client] = value;
    }

    private async Task BeginCreate()
    {
        _vm.BeginCreate();
        var newClient = new ApplicationClientAdminDto
        {
            Id = 0,
            ClientId = string.Empty,
            Audience = string.Empty,
            AllowedReturnUrls = Array.Empty<string>()
        };
        clients.Insert(0, newClient);
        await grid.InsertRow(newClient);
    }

    private async Task OnRowCreate(ApplicationClientAdminDto client)
    {
        _vm.BeginCreate();
        _vm.EditModel.ClientId = client.ClientId;
        _vm.EditModel.Audience = client.Audience;
        allowedUrlsText = GetUrlsBuffer(client);

        var result = await _vm.SaveAsync();
        NotifyUser(result);

        if (result.RequiresReload)
        {
            await _vm.LoadAsync();
            await grid.Reload();
        }

        if (result.Outcome == ClientsVmOutcome.ValidationError)
        {
            clients.Remove(client);
            await grid.Reload();
        }
    }

    private async Task EditRow(ApplicationClientAdminDto client)
    {
        await grid.EditRow(client);
    }

    private async Task OnRowUpdate(ApplicationClientAdminDto client)
    {
        _vm.BeginEdit(client);
        _vm.EditModel.ClientId = client.ClientId;
        _vm.EditModel.Audience = client.Audience;
        allowedUrlsText = GetUrlsBuffer(client);

        var result = await _vm.SaveAsync();
        NotifyUser(result);

        if (result.RequiresReload)
        {
            await _vm.LoadAsync();
            await grid.Reload();
        }
    }

    private async Task DeleteClient(int id)
    {
        var confirm = await DialogService.Confirm("¿Eliminar el cliente?", "Confirmar", new ConfirmOptions { OkButtonText = "Eliminar", CancelButtonText = "Cancelar", Icon = "warning" });
        if (confirm != true)
        {
            return;
        }

        var result = await _vm.DeleteAsync(id);
        NotifyUser(result);

        if (result.RequiresReload)
        {
            await _vm.LoadAsync();
            await grid.Reload();
        }
    }

    private void CancelEditRow(ApplicationClientAdminDto client)
    {
        grid.CancelEditRow(client);
        _urlsBuffer.Remove(client);
        if (client.Id == 0)
        {
            clients.Remove(client);
        }
    }

    private void ClearFilters()
    {
        grid.Reset(true);
    }

    private void NotifyUser(ClientsVmResult result)
    {
        var severity = result.Outcome switch
        {
            ClientsVmOutcome.Success => NotificationSeverity.Success,
            ClientsVmOutcome.ValidationError => NotificationSeverity.Warning,
            ClientsVmOutcome.Error => NotificationSeverity.Error,
            _ => NotificationSeverity.Info
        };

        NotificationService.Notify(severity, result.Title, result.Message);
    }
}
