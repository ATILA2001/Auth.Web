using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Application.Admin.Dtos;
using Radzen;
using Radzen.Blazor;
using System.ComponentModel.DataAnnotations;

namespace Auth.Web.Components.Admin.Clients;

public partial class Clients : ComponentBase
{
    [Inject] private IAdminClientService ClientService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;

    private ClientsViewModel _vm = null!;
    private ClientFormModel clientForm = new();

    // Expose VM state with same names for Razor binding compatibility
    private List<ApplicationClientAdminDto> clients => _vm.Clients;
    private bool editing => _vm.Editing;
    private ApplicationClientAdminDto editModel => _vm.EditModel;
    private string allowedUrlsText
    {
        get => _vm.AllowedUrlsText;
        set => _vm.AllowedUrlsText = value;
    }
    private string? validationError => _vm.ValidationError;

    protected override void OnInitialized()
    {
        _vm = new ClientsViewModel(ClientService);
    }

    protected override async Task OnInitializedAsync()
    {
        await _vm.LoadAsync();
    }

    private void BeginCreate()
    {
        _vm.BeginCreate();
        SyncFormFromVm();
    }

    private void BeginEdit(ApplicationClientAdminDto dto)
    {
        _vm.BeginEdit(dto);
        SyncFormFromVm();
    }

    private async Task OnSubmitClient()
    {
        _vm.EditModel.ClientId = clientForm.ClientId;
        _vm.EditModel.Audience = clientForm.Audience;
        allowedUrlsText = clientForm.AllowedUrlsText;
        await SaveClient();
    }

    private async Task SaveClient()
    {
        var result = await _vm.SaveAsync();
        NotifyUser(result);

        if (result.RequiresReload)
        {
            await _vm.LoadAsync();
        }
    }

    private void CancelEdit() => _vm.CancelEdit();

    private void SyncFormFromVm()
    {
        clientForm = new ClientFormModel
        {
            ClientId = _vm.EditModel.ClientId ?? string.Empty,
            Audience = _vm.EditModel.Audience ?? string.Empty,
            AllowedUrlsText = allowedUrlsText
        };
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
        }
    }
}

public sealed class ClientFormModel
{
    [Required(ErrorMessage = "ClientId requerido")]
    public string ClientId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Audience requerido")]
    public string Audience { get; set; } = string.Empty;

    public string AllowedUrlsText { get; set; } = string.Empty;
}
