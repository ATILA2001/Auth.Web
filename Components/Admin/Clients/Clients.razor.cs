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

    private bool IsLoading { get; set; }
    private bool IsSaving { get; set; }

    protected override void OnInitialized()
    {
        _vm = new ClientsViewModel(ClientService);
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
            _urlsBuffer.Clear();
            if (reloadGrid && grid is not null)
            {
                await grid.Reload();
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "No se pudieron cargar los clientes.", ex.Message);
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
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
        if (IsLoading || IsSaving)
        {
            return;
        }

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
        if (IsSaving)
        {
            return;
        }

        IsSaving = true;
        try
        {
            _vm.BeginCreate();
            _vm.EditModel.ClientId = client.ClientId;
            _vm.EditModel.Audience = client.Audience;
            allowedUrlsText = GetUrlsBuffer(client);

            var result = await _vm.SaveAsync();
            NotifyUser(result);

            if (result.RequiresReload)
            {
                await LoadAsync(reloadGrid: true);
            }

            if (result.Outcome == ClientsVmOutcome.ValidationError)
            {
                clients.Remove(client);
                await grid.Reload();
            }
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task EditRow(ApplicationClientAdminDto client)
    {
        if (IsLoading || IsSaving)
        {
            return;
        }

        await grid.EditRow(client);
    }

    private async Task OnRowUpdate(ApplicationClientAdminDto client)
    {
        if (IsSaving)
        {
            return;
        }

        IsSaving = true;
        try
        {
            _vm.BeginEdit(client);
            _vm.EditModel.ClientId = client.ClientId;
            _vm.EditModel.Audience = client.Audience;
            allowedUrlsText = GetUrlsBuffer(client);

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

    private async Task DeleteClient(int id)
    {
        if (IsSaving)
        {
            return;
        }

        var confirm = await DialogService.Confirm("¿Eliminar el cliente?", "Confirmar", new ConfirmOptions { OkButtonText = "Eliminar", CancelButtonText = "Cancelar", Icon = "warning" });
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

    private void CancelEditRow(ApplicationClientAdminDto client)
    {
        if (IsSaving)
        {
            return;
        }

        grid.CancelEditRow(client);
        _urlsBuffer.Remove(client);
        if (client.Id == 0)
        {
            clients.Remove(client);
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
