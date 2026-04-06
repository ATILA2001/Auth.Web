using Auth.Web.Application.Admin.Dtos;
using Auth.Web.Services.Abstractions.Admin;

namespace Auth.Web.Components.Admin.Permissions;

/// <summary>
/// Representa una fila en la matriz de permisos: una página con sus acciones habilitadas/deshabilitadas.
/// ActionMap: clave = ActionPermissionId, valor = Id del AreaPagePermission (null si no está asignado).
/// </summary>
public sealed class PermissionMatrixRow
{
    public int PageId { get; init; }
    public string PageName { get; init; } = string.Empty;
    public string PageUrl { get; init; } = string.Empty;
    public string ClientName { get; init; } = string.Empty;
    public Dictionary<int, int?> ActionMap { get; } = new();

    public bool IsEnabled(int actionId) =>
        ActionMap.TryGetValue(actionId, out var id) && id.HasValue;

    public int? GetPermissionId(int actionId) =>
        ActionMap.TryGetValue(actionId, out var id) ? id : null;
}

public sealed class PermissionsViewModel
{
    private readonly IAdminAreaPagePermissionService _permissionService;
    private readonly IAdminAreaService _areaService;
    private readonly IAdminPageService _pageService;
    private readonly IAdminActionPermissionService _actionService;

    public PermissionsViewModel(
        IAdminAreaPagePermissionService permissionService,
        IAdminAreaService areaService,
        IAdminPageService pageService,
        IAdminActionPermissionService actionService)
    {
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _areaService = areaService ?? throw new ArgumentNullException(nameof(areaService));
        _pageService = pageService ?? throw new ArgumentNullException(nameof(pageService));
        _actionService = actionService ?? throw new ArgumentNullException(nameof(actionService));
    }

    public List<AreaAdminDto> Areas { get; private set; } = new();
    public List<PageAdminDto> Pages { get; private set; } = new();
    public List<ActionPermissionAdminDto> Actions { get; private set; } = new();
    public List<PermissionMatrixRow> MatrixRows { get; private set; } = new();

    public async Task LoadAsync(CancellationToken ct = default)
    {
        Areas = (await _areaService.GetAreasAsync(ct)).ToList();
        Pages = (await _pageService.GetPagesAsync(ct)).ToList();
        Actions = (await _actionService.GetActionsAsync(ct)).ToList();
    }

    public async Task LoadMatrixForAreaAsync(int areaId, CancellationToken ct = default)
    {
        var area = Areas.FirstOrDefault(a => a.Id == areaId);
        var areaClientId = area?.ClientId;

        var areaPermissions = (await _permissionService.GetPermissionsByAreaAsync(areaId, ct)).ToList();

        var permLookup = areaPermissions
            .Where(p => p.PageId.HasValue && p.ActionPermissionId.HasValue)
            .ToDictionary(
                p => (p.PageId!.Value, p.ActionPermissionId!.Value),
                p => p.Id);

        // Only show pages belonging to the area's application client.
        // If the area has no client assigned, show all pages so the admin can still work.
        var filteredPages = areaClientId.HasValue
            ? Pages.Where(p => p.ClientId == areaClientId).ToList()
            : Pages;

        MatrixRows = filteredPages
            .OrderBy(p => string.IsNullOrWhiteSpace(p.ClientName) ? "Sin asignar" : p.ClientName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .Select(page =>
            {
                var row = new PermissionMatrixRow
                {
                    PageId = page.Id,
                    PageName = page.Name,
                    PageUrl = page.Url,
                    ClientName = string.IsNullOrWhiteSpace(page.ClientName) ? "Sin asignar" : page.ClientName
                };
                foreach (var action in Actions)
                {
                    var key = (page.Id, action.Id);
                    row.ActionMap[action.Id] = permLookup.TryGetValue(key, out var pid) ? pid : (int?)null;
                }
                return row;
            })
            .ToList();
    }
}
