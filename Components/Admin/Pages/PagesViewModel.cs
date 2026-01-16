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
    public int? CreatedId { get; init; }

    public static PagesVmResult Success(string title, string message, bool requiresReload = true) =>
        new() { Outcome = PagesVmOutcome.Success, Title = title, Message = message, RequiresReload = requiresReload };

    public static PagesVmResult CreateSuccess(string title, string message, int createdId) =>
        new() { Outcome = PagesVmOutcome.Success, Title = title, Message = message, RequiresReload = false, CreatedId = createdId };

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
        EditModel = new PageAdminDto { Id = 0, Name = string.Empty, Url = string.Empty, PermissionCount = 0 };
        EditName = string.Empty;
        EditUrl = string.Empty;
        ValidationError = null;
    }

    public void BeginEdit(PageAdminDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        EditModel = new PageAdminDto { Id = dto.Id, Name = dto.Name, Url = dto.Url, PermissionCount = dto.PermissionCount };
        EditName = dto.Name ?? string.Empty;
        EditUrl = dto.Url ?? string.Empty;
        ValidationError = null;
    }

    public PagesVmResult ValidateOnly(string name, string url)
    {
        ValidationError = null;
        var trimmedName = (name ?? string.Empty).Trim();
        var trimmedUrl = (url ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            ValidationError = "El nombre de la página no puede estar vacío.";
            return PagesVmResult.ValidationFailed("Validación", ValidationError);
        }

        if (string.IsNullOrWhiteSpace(trimmedUrl))
        {
            ValidationError = "La URL de la página no puede estar vacía.";
            return PagesVmResult.ValidationFailed("Validación", ValidationError);
        }

        var duplicateUrl = Pages.Any(p => p.Id != EditModel.Id && string.Equals(p.Url, trimmedUrl, StringComparison.OrdinalIgnoreCase));
        if (duplicateUrl)
        {
            ValidationError = "Ya existe una página con esa URL.";
            return PagesVmResult.ValidationFailed("Validación", ValidationError);
        }

        return PagesVmResult.Success("Válido", "", requiresReload: false);
    }

    public async Task<PagesVmResult> SaveAsync()
    {
        // Reuse validation logic to avoid duplication and rule drift
        var validationResult = ValidateOnly(EditName, EditUrl);
        if (validationResult.Outcome != PagesVmOutcome.Success)
        {
            return validationResult;
        }

        var name = EditName.Trim();
        var url = EditUrl.Trim();

        try
        {
            if (EditModel.Id == 0)
            {
                // CREATE: return CreatedId so UI can set it locally without reload
                var id = await _pageService.CreatePageAsync(name, url);
                if (id != 0)
                {
                    ValidationError = null;
                    return PagesVmResult.CreateSuccess("página creada", $"Se creó '{name}'.", id);
                }
                ValidationError = "Nombre inVálido o duplicado.";
                return PagesVmResult.ValidationFailed("Sin cambios", ValidationError);
            }

            // UPDATE: no reload required; buffer?DTO sync handles display update
            await _pageService.UpdatePageAsync(EditModel.Id, name, url);
            ValidationError = null;
            return PagesVmResult.Success("página actualizada", $"Se actualizó '{name}'.", requiresReload: false);
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
            // DELETE: reload required to remove row from grid
            return PagesVmResult.Success("página eliminada", $"Id {id} removido.", requiresReload: true);
        }
        catch (Exception ex)
        {
            return PagesVmResult.Failed("Error al eliminar página", ex.Message);
        }
    }

    // Legacy: Editing property and CancelEdit method not used by current UI
    // Code-behind calls grid.CancelEditRow directly; consider removal in future cleanup
}
