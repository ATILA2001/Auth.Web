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
    private readonly Dictionary<ApplicationClientAdminDto, string> _clientIdBuffer = new();
    private readonly Dictionary<ApplicationClientAdminDto, string> _audienceBuffer = new();
    private readonly Dictionary<ApplicationClientAdminDto, string?> _landingPageBuffer = new();
    private readonly List<ApplicationClientAdminDto> _clientsToInsert = new();
    private readonly List<ApplicationClientAdminDto> _clientsToUpdate = new();
    private int _tempId = -1;

    private List<ApplicationClientAdminDto> clients => _vm.Clients;
    private string editClientId
    {
        get => _vm.EditClientId;
        set => _vm.EditClientId = value;
    }
    private string editAudience
    {
        get => _vm.EditAudience;
        set => _vm.EditAudience = value;
    }
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
            
            // Clear tracking lists after reload to avoid stale references
            _urlsBuffer.Clear();
            _clientIdBuffer.Clear();
            _audienceBuffer.Clear();
            _landingPageBuffer.Clear();
            _clientsToInsert.Clear();
            _clientsToUpdate.Clear();
            
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

    private string GetClientIdBuffer(ApplicationClientAdminDto client)
    {
        if (!_clientIdBuffer.TryGetValue(client, out var value))
        {
            value = client.ClientId;
            _clientIdBuffer[client] = value;
        }
        return value;
    }

    private void SetClientIdBuffer(ApplicationClientAdminDto client, string value)
    {
        _clientIdBuffer[client] = value;
    }

    private string GetAudienceBuffer(ApplicationClientAdminDto client)
    {
        if (!_audienceBuffer.TryGetValue(client, out var value))
        {
            value = client.Audience;
            _audienceBuffer[client] = value;
        }
        return value;
    }

    private void SetAudienceBuffer(ApplicationClientAdminDto client, string value)
    {
        _audienceBuffer[client] = value;
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

    private string? GetLandingPageBuffer(ApplicationClientAdminDto client)
    {
        if (!_landingPageBuffer.TryGetValue(client, out var value))
        {
            value = client.DefaultLandingPage;
            _landingPageBuffer[client] = value;
        }
        return value;
    }

    private void SetLandingPageBuffer(ApplicationClientAdminDto client, string? value)
    {
        _landingPageBuffer[client] = value;
    }

    private async Task BeginCreate()
    {
        if (IsLoading || IsSaving)
        {
            return;
        }

        // Prevent multiple pending CREATE rows (single-create-at-a-time)
        if (_clientsToInsert.Count > 0)
        {
            return;
        }

        _vm.BeginCreate();
        var newClient = new ApplicationClientAdminDto
        {
            Id = _tempId--,
            ClientId = string.Empty,
            Audience = string.Empty,
            AllowedReturnUrls = Array.Empty<string>(),
            DefaultLandingPage = null
        };
        _clientsToInsert.Add(newClient);
        await grid.InsertRow(newClient);
    }

    private async Task ValidateAndSave(ApplicationClientAdminDto client)
    {
        if (IsLoading || IsSaving)
        {
            return;
        }

        // Set VM context for validation: for EDIT set BeginEdit; for CREATE EditFields already set from buffer
        if (client.Id > 0)
        {
            _vm.BeginEdit(client);
        }
        else
        {
            // For CREATE: ensure EditFields are synced from buffer for pre-validation
            _vm.BeginCreate();
            _vm.EditClientId = GetClientIdBuffer(client);
            _vm.EditAudience = GetAudienceBuffer(client);
            _vm.AllowedUrlsText = GetUrlsBuffer(client);
            _vm.EditDefaultLandingPage = GetLandingPageBuffer(client);
        }

        var clientId = GetClientIdBuffer(client);
        var audience = GetAudienceBuffer(client);
        var urlsText = GetUrlsBuffer(client);
        var validationResult = _vm.ValidateOnly(clientId, audience, urlsText);

        if (validationResult.Outcome != ClientsVmOutcome.Success)
        {
            NotifyUser(validationResult);
            // For CREATE: Do NOT call grid.UpdateRow - validation failed before persistence
            // Keep the row in edit mode by not exiting; grid is already in edit mode
            return;
        }

        await grid.UpdateRow(client);
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
            // EditFields already set from ValidateAndSave; just call SaveAsync
            editClientId = GetClientIdBuffer(client);
            editAudience = GetAudienceBuffer(client);
            allowedUrlsText = GetUrlsBuffer(client);
            _vm.EditDefaultLandingPage = GetLandingPageBuffer(client);
            var result = await _vm.SaveAsync();
            NotifyUser(result);

            if (result.Outcome == ClientsVmOutcome.Success)
            {
                // CRITICAL: Sync buffer ? DTO so grid displays the new values in view mode
                client.ClientId = editClientId.Trim();
                client.Audience = editAudience.Trim();
                client.AllowedReturnUrls = ClientsViewModel.NormalizeUrls(allowedUrlsText);
                client.DefaultLandingPage = _vm.EditDefaultLandingPage;
                
                // Apply CreatedId from service (no reload needed)
                if (result.CreatedId.HasValue)
                {
                    client.Id = result.CreatedId.Value;
                }
                
                _clientsToInsert.Remove(client);
                _clientIdBuffer.Remove(client);
                _audienceBuffer.Remove(client);
                _urlsBuffer.Remove(client);
                _landingPageBuffer.Remove(client);
                
                // Only reload if CreatedId is missing (fallback)
                if (!result.CreatedId.HasValue && result.RequiresReload)
                {
                    await LoadAsync(reloadGrid: true);
                }
            }
            // Note: ValidationError case removed - pre-validation in ValidateAndSave prevents reaching this point
            // If SaveAsync returns ValidationError here, it's a service-layer issue, not UI validation
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

        if (!_clientsToUpdate.Contains(client))
        {
            _clientsToUpdate.Add(client);
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
            editClientId = GetClientIdBuffer(client);
            editAudience = GetAudienceBuffer(client);
            allowedUrlsText = GetUrlsBuffer(client);
            _vm.EditDefaultLandingPage = GetLandingPageBuffer(client);
            var result = await _vm.SaveAsync();
            NotifyUser(result);

            if (result.Outcome == ClientsVmOutcome.Success)
            {
                // CRITICAL: Sync buffer ? DTO so grid displays the updated values in view mode
                client.ClientId = editClientId.Trim();
                client.Audience = editAudience.Trim();
                client.AllowedReturnUrls = ClientsViewModel.NormalizeUrls(allowedUrlsText);
                client.DefaultLandingPage = _vm.EditDefaultLandingPage;
                
                _clientsToUpdate.Remove(client);
                _clientIdBuffer.Remove(client);
                _audienceBuffer.Remove(client);
                _urlsBuffer.Remove(client);
                _landingPageBuffer.Remove(client);
                
                // Explicit contract: UPDATE success does NOT require reload (RequiresReload=false)
                // Filters/pagination/sorting preserved via local update
                await InvokeAsync(StateHasChanged);
            }
            // Note: ValidationError case removed - pre-validation in ValidateAndSave prevents reaching this point
            // If SaveAsync returns ValidationError here, it's a service-layer issue, not UI validation
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

        var confirm = await DialogService.Confirm("Eliminar el cliente?", "Confirmar", new ConfirmOptions { OkButtonText = "Eliminar", CancelButtonText = "Cancelar", Icon = "warning" });
        if (confirm != true)
        {
            return;
        }

        IsSaving = true;
        try
        {
            var result = await _vm.DeleteAsync(id);
            NotifyUser(result);

            // Explicit contract: DELETE success requires reload to remove row from grid
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
        _clientIdBuffer.Remove(client);
        _audienceBuffer.Remove(client);
        _landingPageBuffer.Remove(client);
        _clientsToInsert.Remove(client);
        _clientsToUpdate.Remove(client);
        
        if (client.Id <= 0)
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
