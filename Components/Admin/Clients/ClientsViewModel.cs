using Auth.Web.Application.Admin.Dtos;
using Auth.Web.Services.Abstractions.Admin;

namespace Auth.Web.Components.Admin.Clients;

public enum ClientsVmOutcome
{
    Success,
    ValidationError,
    Error
}

public sealed class ClientsVmResult
{
    public ClientsVmOutcome Outcome { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public bool RequiresReload { get; init; }
    public int? CreatedId { get; init; }

    public static ClientsVmResult Success(string title, string message, bool requiresReload = true) =>
        new() { Outcome = ClientsVmOutcome.Success, Title = title, Message = message, RequiresReload = requiresReload };

    public static ClientsVmResult CreateSuccess(string title, string message, int createdId) =>
        new() { Outcome = ClientsVmOutcome.Success, Title = title, Message = message, RequiresReload = false, CreatedId = createdId };

    public static ClientsVmResult ValidationFailed(string title, string message) =>
        new() { Outcome = ClientsVmOutcome.ValidationError, Title = title, Message = message, RequiresReload = false };

    public static ClientsVmResult Failed(string title, string message) =>
        new() { Outcome = ClientsVmOutcome.Error, Title = title, Message = message, RequiresReload = false };
}

public sealed class ClientsViewModel
{
    private readonly IAdminClientService _clientService;

    public ClientsViewModel(IAdminClientService clientService)
    {
        ArgumentNullException.ThrowIfNull(clientService);
        _clientService = clientService;
    }

    public List<ApplicationClientAdminDto> Clients { get; private set; } = new();
    public ApplicationClientAdminDto EditModel { get; private set; } = new();
    public string EditClientId { get; set; } = string.Empty;
    public string EditAudience { get; set; } = string.Empty;
    public string AllowedUrlsText { get; set; } = string.Empty;
    public string? ValidationError { get; private set; }

    public async Task LoadAsync()
    {
        Clients = (await _clientService.GetClientsAsync()).ToList();
    }

    public void BeginCreate()
    {
        EditModel = new ApplicationClientAdminDto
        {
            Id = 0,
            ClientId = string.Empty,
            Audience = string.Empty,
            AllowedReturnUrls = Array.Empty<string>()
        };
        EditClientId = string.Empty;
        EditAudience = string.Empty;
        AllowedUrlsText = string.Empty;
        ValidationError = null;
    }

    public void BeginEdit(ApplicationClientAdminDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        EditModel = new ApplicationClientAdminDto
        {
            Id = dto.Id,
            ClientId = dto.ClientId,
            Audience = dto.Audience,
            AllowedReturnUrls = dto.AllowedReturnUrls.ToArray()
        };
        EditClientId = dto.ClientId ?? string.Empty;
        EditAudience = dto.Audience ?? string.Empty;
        AllowedUrlsText = string.Join("\n", dto.AllowedReturnUrls);
        ValidationError = null;
    }

    public ClientsVmResult ValidateOnly(string clientId, string audience, string urlsText)
    {
        ValidationError = null;
        var trimmedClientId = (clientId ?? string.Empty).Trim();
        var trimmedAudience = (audience ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(trimmedClientId))
        {
            ValidationError = "El ClientId no puede estar vacío.";
            return ClientsVmResult.ValidationFailed("Validación", ValidationError);
        }

        if (string.IsNullOrWhiteSpace(trimmedAudience))
        {
            ValidationError = "El Audience no puede estar vacío.";
            return ClientsVmResult.ValidationFailed("Validación", ValidationError);
        }

        var duplicate = Clients.Any(c => c.Id != EditModel.Id && 
                                         string.Equals(c.ClientId, trimmedClientId, StringComparison.OrdinalIgnoreCase));
        if (duplicate)
        {
            ValidationError = "Ya existe un cliente con ese ClientId.";
            return ClientsVmResult.ValidationFailed("Validación", ValidationError);
        }

        return ClientsVmResult.Success("Válido", "", requiresReload: false);
    }

    public async Task<ClientsVmResult> SaveAsync()
    {
        // Reuse validation logic to avoid duplication and rule drift
        var validationResult = ValidateOnly(EditClientId, EditAudience, AllowedUrlsText);
        if (validationResult.Outcome != ClientsVmOutcome.Success)
        {
            return validationResult;
        }

        var clientId = EditClientId.Trim();
        var audience = EditAudience.Trim();
        var urls = NormalizeUrls(AllowedUrlsText);

        try
        {
            if (EditModel.Id == 0)
            {
                // CREATE: return CreatedId so UI can set it locally without reload
                var id = await _clientService.CreateClientAsync(clientId, audience, urls);
                if (id != 0)
                {
                    ValidationError = null;
                    return ClientsVmResult.CreateSuccess("Cliente creado", $"Se creó '{clientId}'.", id);
                }
                ValidationError = "Nombre inVálido o duplicado.";
                return ClientsVmResult.ValidationFailed("Sin cambios", ValidationError);
            }

            // UPDATE: no reload required; buffer?DTO sync handles display update
            await _clientService.UpdateClientAsync(EditModel.Id, clientId, audience, urls);
            ValidationError = null;
            return ClientsVmResult.Success("Cliente actualizado", $"Se actualizó '{clientId}'.", requiresReload: false);
        }
        catch (Exception ex)
        {
            ValidationError = ex.Message;
            return ClientsVmResult.Failed("Error al guardar cliente", ex.Message);
        }
    }

    public async Task<ClientsVmResult> DeleteAsync(int id)
    {
        try
        {
            await _clientService.DeleteClientAsync(id);
            // DELETE: reload required to remove row from grid
            return ClientsVmResult.Success("Cliente eliminado", $"Id {id} removido.", requiresReload: true);
        }
        catch (Exception ex)
        {
            return ClientsVmResult.Failed("Error al eliminar cliente", ex.Message);
        }
    }

    public static string[] NormalizeUrls(string text)
    {
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return lines.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    // Legacy: Editing property and CancelEdit method not used by current UI
    // Code-behind calls grid.CancelEditRow directly; consider removal in future cleanup
}
