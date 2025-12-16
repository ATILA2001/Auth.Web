using Microsoft.AspNetCore.Components;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Application.Admin.Dtos;
using Radzen;

namespace Auth.Web.Components.Admin.Clients;

public partial class Clients : ComponentBase
{
    [Inject] private IAdminClientService ClientService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;

    private ClientsViewModel _vm = null!;

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

    private void BeginCreate() => _vm.BeginCreate();

    private void BeginEdit(ApplicationClientAdminDto dto) => _vm.BeginEdit(dto);

    private async Task SaveClient()
    {
        var result = await _vm.SaveAsync();
        NotifyUser(result);

        if (result.RequiresReload)
        {
            await _vm.LoadAsync();
        }
    }

    private async Task DeleteClient(int id)
    {
        var result = await _vm.DeleteAsync(id);
        NotifyUser(result);

        if (result.RequiresReload)
        {
            await _vm.LoadAsync();
        }
    }

    private void CancelEdit() => _vm.CancelEdit();

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
