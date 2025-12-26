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

    private bool IsLoading { get; set; }
    private bool IsSaving { get; set; }

    protected override void OnInitialized()
    {
        _vm = new PagesViewModel(AdminPageService);
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
            if (reloadGrid && grid is not null)
            {
                await grid.Reload();
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "No se pudieron cargar las páginas.", ex.Message);
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private async Task BeginCreate()
    {
        if (IsLoading || IsSaving)
        {
            return;
        }

        _vm.BeginCreate();
        var newPage = new PageAdminDto { Id = 0, Name = string.Empty, Url = string.Empty, PermissionCount = 0 };
        pages.Insert(0, newPage);
        await grid.InsertRow(newPage);
    }

    private async Task OnRowCreate(PageAdminDto page)
    {
        if (IsSaving)
        {
            return;
        }

        IsSaving = true;
        try
        {
            _vm.BeginCreate();
            editName = page.Name;
            editUrl = page.Url;
            var result = await _vm.SaveAsync();
            NotifyUser(result);

            if (result.RequiresReload)
            {
                await LoadAsync(reloadGrid: true);
            }

            if (result.Outcome == PagesVmOutcome.ValidationError)
            {
                pages.Remove(page);
                await grid.Reload();
            }
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task EditRow(PageAdminDto page)
    {
        if (IsLoading || IsSaving)
        {
            return;
        }

        await grid.EditRow(page);
    }

    private async Task OnRowUpdate(PageAdminDto page)
    {
        if (IsSaving)
        {
            return;
        }

        IsSaving = true;
        try
        {
            _vm.BeginEdit(page);
            editName = page.Name;
            editUrl = page.Url;
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

    private async Task DeletePage(int id)
    {
        if (IsSaving)
        {
            return;
        }

        var confirm = await DialogService.Confirm("¿Eliminar la página?", "Confirmar", new ConfirmOptions { OkButtonText = "Eliminar", CancelButtonText = "Cancelar", Icon = "warning" });
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

    private void CancelEditRow(PageAdminDto page)
    {
        if (IsSaving)
        {
            return;
        }

        grid.CancelEditRow(page);
        if (page.Id == 0)
        {
            pages.Remove(page);
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
