using Microsoft.AspNetCore.Components;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Application.Admin.Dtos;
using Radzen;
using Radzen.Blazor;

namespace Auth.Web.Components.Admin.Pages;

public partial class Pages : ComponentBase
{
    [Inject] private IAdminPageService AdminPageService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;

    private PagesViewModel _vm = null!;
    private RadzenDataGrid<PageAdminDto> grid = null!;

    private List<PageAdminDto> pages => _vm.Pages;
    private bool editing => _vm.Editing;
    private PageAdminDto editModel => _vm.EditModel;
    private string editName
    {
        get => _vm.EditName;
        set => _vm.EditName = value;
    }
    private string editUrl
    {
        get => _vm.EditUrl;
        set => _vm.EditUrl = value;
    }
    private string? validationError => _vm.ValidationError;

    protected override void OnInitialized()
    {
        _vm = new PagesViewModel(AdminPageService);
    }

    protected override async Task OnInitializedAsync()
    {
        await _vm.LoadAsync();
    }

    private void BeginCreate() => _vm.BeginCreate();

    private void BeginEdit(PageAdminDto dto) => _vm.BeginEdit(dto);

    private async Task SavePage()
    {
        var result = await _vm.SaveAsync();
        NotifyUser(result);

        if (result.RequiresReload)
        {
            await _vm.LoadAsync();
            await grid.Reload();
        }
    }

    private async Task DeletePage(int id)
    {
        var confirm = await DialogService.Confirm("¿Eliminar la página?", "Confirmar", new ConfirmOptions { OkButtonText = "Eliminar", CancelButtonText = "Cancelar", Icon = "warning" });
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

    private void CancelEdit() => _vm.CancelEdit();

    private void NotifyUser(PagesVmResult result)
    {
        var severity = result.Outcome switch
        {
            PagesVmOutcome.Success => NotificationSeverity.Success,
            PagesVmOutcome.ValidationError => NotificationSeverity.Warning,
            PagesVmOutcome.Error => NotificationSeverity.Error,
            _ => NotificationSeverity.Info
        };

        NotificationService.Notify(severity, result.Title, result.Message);
    }
}
