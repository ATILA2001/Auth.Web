using Auth.Web.Application.Admin.Dtos;
using Auth.Web.Services.Abstractions.Admin;

namespace Auth.Web.Components.Admin.Pages;

public enum PagesVmOutcome
{
    Success,
    ValidationError,
    Error
}

public sealed class PagesVmResult
{
    public PagesVmOutcome Outcome { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public bool RequiresReload { get; init; }

    public static PagesVmResult Success(string title, string message, bool requiresReload = true) =>
        new() { Outcome = PagesVmOutcome.Success, Title = title, Message = message, RequiresReload = requiresReload };

    public static PagesVmResult ValidationFailed(string title, string message) =>
        new() { Outcome = PagesVmOutcome.ValidationError, Title = title, Message = message, RequiresReload = false };

    public static PagesVmResult Failed(string title, string message) =>
        new() { Outcome = PagesVmOutcome.Error, Title = title, Message = message, RequiresReload = false };
}

public sealed class PagesViewModel
{
    private readonly IAdminPageService _pageService;

    public PagesViewModel(IAdminPageService pageService)
    {
        ArgumentNullException.ThrowIfNull(pageService);
        _pageService = pageService;
    }

    public List<PageAdminDto> Pages { get; private set; } = new();
    public bool Editing { get; private set; }
    public PageAdminDto EditModel { get; private set; } = new();
    public string EditName { get; set; } = string.Empty;
    public string EditUrl { get; set; } = string.Empty;
    public string? ValidationError { get; private set; }

    public async Task LoadAsync()
    {
        Pages = (await _pageService.GetPagesAsync()).ToList();
    }

    public void BeginCreate()
    {
        EditModel = new PageAdminDto { Id = 0 };
        EditName = string.Empty;
        EditUrl = string.Empty;
        ValidationError = null;
        Editing = true;
    }

    public void BeginEdit(PageAdminDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        EditModel = new PageAdminDto { Id = dto.Id };
        EditName = dto.Name ?? string.Empty;
        EditUrl = dto.Url ?? string.Empty;
        ValidationError = null;
        Editing = true;
    }

    public async Task<PagesVmResult> SaveAsync()
    {
        ValidationError = null;

        var name = (EditName ?? string.Empty).Trim();
        var url = (EditUrl ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(url))
        {
            ValidationError = "Completa todos los campos";
            return PagesVmResult.ValidationFailed("Validación", ValidationError);
        }

        

        var duplicateUrl = Pages.Any(p => p.Id != EditModel.Id && string.Equals(p.Url, url, StringComparison.OrdinalIgnoreCase));
        if (duplicateUrl)
        {
            ValidationError = "Ya existe una página con esa URL";
            return PagesVmResult.ValidationFailed("Validación", ValidationError);
        }

        try
        {
            if (EditModel.Id == 0)
            {
                var id = await _pageService.CreatePageAsync(name, url);
                Editing = false;
                ValidationError = null;
                return PagesVmResult.Success("Página creada", $"Id {id} creada.");
            }

            await _pageService.UpdatePageAsync(EditModel.Id, name, url);
            Editing = false;
            ValidationError = null;
            return PagesVmResult.Success("Página actualizada", $"Id {EditModel.Id} actualizada.");
        }
        catch (Exception ex)
        {
            ValidationError = ex.Message;
            return PagesVmResult.Failed("Error al guardar página", ex.Message);
        }
    }

    public async Task<PagesVmResult> DeleteAsync(int id)
    {
        try
        {
            await _pageService.DeletePageAsync(id);
            return PagesVmResult.Success("Página eliminada", $"Id {id} eliminada.");
        }
        catch (Exception ex)
        {
            return PagesVmResult.Failed("Error al eliminar página", ex.Message);
        }
    }

    public void CancelEdit()
    {
        Editing = false;
        ValidationError = null;
        EditName = string.Empty;
        EditUrl = string.Empty;
        EditModel = new PageAdminDto { Id = 0 };
    }
}
