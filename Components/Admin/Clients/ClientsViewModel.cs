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

    public static ClientsVmResult Success(string title, string message, bool requiresReload = true) =>
        new() { Outcome = ClientsVmOutcome.Success, Title = title, Message = message, RequiresReload = requiresReload };

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
    public bool Editing { get; private set; }
    public ApplicationClientAdminDto EditModel { get; private set; } = new();
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
        AllowedUrlsText = string.Empty;
        ValidationError = null;
        Editing = true;
    }

    public void BeginEdit(ApplicationClientAdminDto dto)
    {
        EditModel = new ApplicationClientAdminDto
        {
            Id = dto.Id,
            ClientId = dto.ClientId,
            Audience = dto.Audience,
            AllowedReturnUrls = dto.AllowedReturnUrls.ToArray()
        };
        AllowedUrlsText = string.Join("\n", dto.AllowedReturnUrls);
        ValidationError = null;
        Editing = true;
    }

    public async Task<ClientsVmResult> SaveAsync()
    {
        ValidationError = null;

        if (string.IsNullOrWhiteSpace(EditModel.ClientId))
        {
            ValidationError = "El ClientId no puede estar vacío.";
            return ClientsVmResult.ValidationFailed("Validación", ValidationError);
        }

        if (string.IsNullOrWhiteSpace(EditModel.Audience))
        {
            ValidationError = "El Audience no puede estar vacío.";
            return ClientsVmResult.ValidationFailed("Validación", ValidationError);
        }

        var urls = NormalizeUrls(AllowedUrlsText);

        try
        {
            if (EditModel.Id == 0)
            {
                var id = await _clientService.CreateClientAsync(EditModel.ClientId, EditModel.Audience, urls);
                Editing = false;
                return ClientsVmResult.Success("Cliente creado", $"Se creó '{EditModel.ClientId}' (Id {id}).");
            }
            else
            {
                await _clientService.UpdateClientAsync(EditModel.Id, EditModel.ClientId, EditModel.Audience, urls);
                Editing = false;
                return ClientsVmResult.Success("Cliente actualizado", $"Se actualizó '{EditModel.ClientId}'.");
            }
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
            return ClientsVmResult.Success("Cliente eliminado", $"Id {id} eliminado.");
        }
        catch (Exception ex)
        {
            return ClientsVmResult.Failed("Error al eliminar cliente", ex.Message);
        }
    }

    public void CancelEdit()
    {
        Editing = false;
        ValidationError = null;
    }

    private static string[] NormalizeUrls(string text)
    {
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return lines.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }
}
