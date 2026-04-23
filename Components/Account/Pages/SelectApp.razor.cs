using System.Security.Claims;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Services.Abstractions.Auth.Models;
using Auth.Web.Services.Abstractions.Routing;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Infrastructure;

namespace Auth.Web.Components.Account.Pages;

public partial class SelectApp : ComponentBase, IDisposable
{
    private const string AntiforgeryStateKey = "select-app-antiforgery";

    private List<AppPickerOption> _apps = [];
    private string? _antiforgeryFieldName;
    private string? _antiforgeryToken;
    private PersistingComponentStateSubscription? _subscription;

    [Inject] private IRoutingService RoutingService { get; set; } = default!;
    [Inject] private IAdminClientService AdminClientService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private IAntiforgery Antiforgery { get; set; } = default!;
    [Inject] private IHttpContextAccessor HttpContextAccessor { get; set; } = default!;
    [Inject] private PersistentComponentState ApplicationState { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    public string? AntiforgeryFieldName => _antiforgeryFieldName;
    public string? AntiforgeryToken => _antiforgeryToken;
    public string? ErrorMessage { get; private set; }
    public bool IsAdmin { get; private set; }

    protected override async Task OnInitializedAsync()
    {
        _subscription = ApplicationState.RegisterOnPersisting(PersistAntiforgeryTokenAsync);

        if (!TryLoadPersistedTokens())
        {
            GenerateTokensFromHttpContext();
        }

        var uri = new Uri(NavigationManager.Uri);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        if (query["error"] == "access_denied")
        {
            ErrorMessage = "No tiene acceso a la aplicación seleccionada.";
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

    private bool TryLoadPersistedTokens()
    {
        if (ApplicationState.TryTakeFromJson<AntiforgeryPayload>(AntiforgeryStateKey, out var payload)
            && payload is not null
            && !string.IsNullOrEmpty(payload.FieldName)
            && !string.IsNullOrEmpty(payload.Token))
        {
            _antiforgeryFieldName = payload.FieldName;
            _antiforgeryToken = payload.Token;
            return true;
        }
        return false;
    }

    private void GenerateTokensFromHttpContext()
    {
        var httpContext = HttpContextAccessor.HttpContext;
        if (httpContext is null) return;

        var tokens = Antiforgery.GetAndStoreTokens(httpContext);
        _antiforgeryFieldName = tokens.FormFieldName;
        _antiforgeryToken = tokens.RequestToken;
    }

    private Task PersistAntiforgeryTokenAsync()
    {
        if (string.IsNullOrEmpty(_antiforgeryFieldName) || string.IsNullOrEmpty(_antiforgeryToken))
        {
            GenerateTokensFromHttpContext();
        }

        if (!string.IsNullOrEmpty(_antiforgeryFieldName) && !string.IsNullOrEmpty(_antiforgeryToken))
        {
            ApplicationState.PersistAsJson(AntiforgeryStateKey, new AntiforgeryPayload(_antiforgeryFieldName, _antiforgeryToken));
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }

    private sealed record AntiforgeryPayload(string FieldName, string Token);
}
