using Microsoft.AspNetCore.Components;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Application.Admin.Dtos;
using Radzen;
using Radzen.Blazor;

namespace Auth.Web.Components.Admin.Pages;

public partial class Pages : ComponentBase
{
    [Inject] private IAdminPageService AdminPageService { get; set; } = null!;
    [Inject] private IAdminClientService AdminClientService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;

    private PagesViewModel _vm = null!;
    private RadzenDataGrid<PageAdminDto> grid = null!;
    private readonly Dictionary<PageAdminDto, string> _nameBuffer = new();
    private readonly Dictionary<PageAdminDto, string> _urlBuffer = new();
    private readonly Dictionary<PageAdminDto, int?> _clientBuffer = new();
    private readonly List<PageAdminDto> _pagesToInsert = new();
    private readonly List<PageAdminDto> _pagesToUpdate = new();

    private List<PageAdminDto> pages => _vm.Pages;
    private List<ApplicationClientAdminDto> clients => _vm.Clients;

    private bool IsLoading { get; set; }
    private bool IsSaving { get; set; }

    protected override void OnInitialized()
    {
        _vm = new PagesViewModel(AdminPageService, AdminClientService);
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

            _nameBuffer.Clear();
            _urlBuffer.Clear();
            _clientBuffer.Clear();
            _pagesToInsert.Clear();
            _pagesToUpdate.Clear();

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

    private string GetNameBuffer(PageAdminDto page)
    {
        if (!_nameBuffer.TryGetValue(page, out var value))
        {
            value = page.Name;
            _nameBuffer[page] = value;
        }
        return value;
    }

    private void SetNameBuffer(PageAdminDto page, string value)
    {
        _nameBuffer[page] = value;
    }

    private string GetUrlBuffer(PageAdminDto page)
    {
        if (!_urlBuffer.TryGetValue(page, out var value))
        {
            value = page.Url;
            _urlBuffer[page] = value;
        }
        return value;
    }

    private void SetUrlBuffer(PageAdminDto page, string value)
    {
        _urlBuffer[page] = value;
    }

    private int? GetClientBuffer(PageAdminDto page)
    {
        if (!_clientBuffer.TryGetValue(page, out var value))
        {
            value = page.ClientId;
            _clientBuffer[page] = value;
        }
        return value;
    }

    private void SetClientBuffer(PageAdminDto page, int? value)
    {
        _clientBuffer[page] = value;
        page.ClientName = value.HasValue
            ? clients.FirstOrDefault(c => c.Id == value.Value)?.ClientId ?? string.Empty
            : string.Empty;
    }

    private async Task BeginCreate()
    {
        if (IsLoading || IsSaving)
        {
            return;
        }

        if (_pagesToInsert.Count > 0)
        {
            return;
        }

        _vm.BeginCreate();
        var newPage = new PageAdminDto { Id = 0, Name = string.Empty, Url = string.Empty, PermissionCount = 0 };
        _pagesToInsert.Add(newPage);
        pages.Insert(0, newPage);
        await grid.InsertRow(newPage);
    }

    private async Task ValidateAndSave(PageAdminDto page)
    {
        if (IsLoading || IsSaving)
        {
            return;
        }

        if (page.Id != 0)
        {
            _vm.BeginEdit(page);
        }
        else
        {
            _vm.EditName = GetNameBuffer(page);
            _vm.EditUrl = GetUrlBuffer(page);
            _vm.EditClientId = GetClientBuffer(page);
        }

        var name = GetNameBuffer(page);
        var url = GetUrlBuffer(page);
        var validationResult = _vm.ValidateOnly(name, url);

        if (validationResult.Outcome != PagesVmOutcome.Success)
        {
            NotifyUser(validationResult);
            return;
        }

        await grid.UpdateRow(page);
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
            _vm.EditName = GetNameBuffer(page);
            _vm.EditUrl = GetUrlBuffer(page);
            _vm.EditClientId = GetClientBuffer(page);
            var result = await _vm.SaveAsync();
            NotifyUser(result);

            if (result.Outcome == PagesVmOutcome.Success)
            {
                page.Name = _vm.EditName.Trim();
                page.Url = _vm.EditUrl.Trim();
                page.ClientId = _vm.EditClientId;
                page.ClientName = _vm.EditClientId.HasValue
                    ? clients.FirstOrDefault(c => c.Id == _vm.EditClientId.Value)?.ClientId ?? string.Empty
                    : string.Empty;

                if (result.CreatedId.HasValue)
                {
                    page.Id = result.CreatedId.Value;
                }

                _pagesToInsert.Remove(page);
                _nameBuffer.Remove(page);
                _urlBuffer.Remove(page);
                _clientBuffer.Remove(page);

                if (!result.CreatedId.HasValue && result.RequiresReload)
                {
                    await LoadAsync(reloadGrid: true);
                }
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

        if (!_pagesToUpdate.Contains(page))
        {
            _pagesToUpdate.Add(page);
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
            _vm.EditName = GetNameBuffer(page);
            _vm.EditUrl = GetUrlBuffer(page);
            _vm.EditClientId = GetClientBuffer(page);
            var result = await _vm.SaveAsync();
            NotifyUser(result);

            if (result.Outcome == PagesVmOutcome.Success)
            {
                page.Name = _vm.EditName.Trim();
                page.Url = _vm.EditUrl.Trim();
                page.ClientId = _vm.EditClientId;
                page.ClientName = _vm.EditClientId.HasValue
                    ? clients.FirstOrDefault(c => c.Id == _vm.EditClientId.Value)?.ClientId ?? string.Empty
                    : string.Empty;

                _pagesToUpdate.Remove(page);
                _nameBuffer.Remove(page);
                _urlBuffer.Remove(page);
                _clientBuffer.Remove(page);

                await InvokeAsync(StateHasChanged);
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
        _nameBuffer.Remove(page);
        _urlBuffer.Remove(page);
        _clientBuffer.Remove(page);
        _pagesToInsert.Remove(page);
        _pagesToUpdate.Remove(page);

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
