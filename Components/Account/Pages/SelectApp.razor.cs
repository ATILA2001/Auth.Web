using System.Security.Claims;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Services.Abstractions.Auth.Models;
using Auth.Web.Services.Abstractions.Routing;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Auth.Web.Components.Account.Pages;

public partial class SelectApp : ComponentBase
{
    private List<AppPickerOption> _apps = [];

    [Inject] private IRoutingService RoutingService { get; set; } = default!;
    [Inject] private IAdminClientService AdminClientService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IAntiforgery Antiforgery { get; set; } = default!;
    [Inject] private IHttpContextAccessor HttpContextAccessor { get; set; } = default!;

    public string? ErrorMessage { get; private set; }
    public bool IsAdmin { get; private set; }
    public string? AntiforgeryFieldName { get; private set; }
    public string? AntiforgeryToken { get; private set; }

    protected override async Task OnInitializedAsync()
    {
        var uri = new Uri(NavigationManager.Uri);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        if (query["error"] == "access_denied")
        {
            ErrorMessage = "No tiene acceso a la aplicación seleccionada.";
        }

        // Generate antiforgery tokens for the logout POST form.
        // HttpContext is available here because App.razor.cs forces SSR (null render mode)
        // for all /Account/* routes, so this component always runs server-side.
        var httpContext = HttpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var tokens = Antiforgery.GetAndStoreTokens(httpContext);
            AntiforgeryFieldName = tokens.FormFieldName;
            AntiforgeryToken = tokens.RequestToken;
        }

        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

        IsAdmin = user.IsInRole("Admin") || user.IsInRole("Administrador");

        if (!string.IsNullOrWhiteSpace(userId))
        {
            if (IsAdmin)
            {
                var allClients = await AdminClientService.GetClientsAsync();
                _apps = allClients
                    .Select(c => new AppPickerOption(
                        c.ClientId,
                        c.AllowedReturnUrls.FirstOrDefault() ?? string.Empty,
                        GetAppDisplayName(c.ClientId)))
                    .Where(a => !string.IsNullOrWhiteSpace(a.ReturnUrl))
                    .ToList();
            }
            else
            {
                var routes = await RoutingService.ResolveAllForUserAsync(userId);
                _apps = routes
                    .Select(r => new AppPickerOption(r.ClientId, r.ReturnUrl, GetAppDisplayName(r.ClientId)))
                    .ToList();
            }
        }
    }

    private static string GetAppDisplayName(string clientId) => clientId switch
    {
        "sai" => "Sistema de Administración de Inventario",
        "PlaniLocal" => "Administración Financiera",
        _ => clientId
    };
}
