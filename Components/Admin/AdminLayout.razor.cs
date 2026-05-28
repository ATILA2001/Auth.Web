using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Auth.Web.Application.Admin.Dtos;
using Auth.Web.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Radzen;

namespace Auth.Web.Components.Admin;

public partial class AdminLayout : LayoutComponentBase, IDisposable
{
    private const string AntiforgeryStateKey = "admin-logout-antiforgery";

    bool sidebarExpanded = true;

    private List<ApplicationClientAdminDto> clients = new();
    private bool _appSwitcherOpen;
    private string? CurrentUserName = string.Empty;
    private string? _antiforgeryFieldName;
    private string? _antiforgeryToken;
    private bool _canSubmitLogout;
    private PersistingComponentStateSubscription? _subscription;

    [Inject]
    private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

    [Inject]
    private UserManager<ApplicationUser> UserManager { get; set; } = default!;

    [Inject]
    private Services.Abstractions.Admin.IAdminClientService AdminClientService { get; set; } = default!;

    [Inject]
    private IAntiforgery Antiforgery { get; set; } = default!;

    [Inject]
    private IHttpContextAccessor HttpContextAccessor { get; set; } = default!;

    [Inject]
    private PersistentComponentState ApplicationState { get; set; } = default!;

    [Inject]
    private NotificationService NotificationService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _subscription = ApplicationState.RegisterOnPersisting(PersistAntiforgeryTokensAsync);

            if (!TryLoadPersistedTokens())
            {
                GenerateTokensFromHttpContext();
            }

            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var user = await UserManager.GetUserAsync(authState.User);
            CurrentUserName = GetDisplayName(user);

            clients = (await AdminClientService.GetClientsAsync()).ToList();
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "No se pudo cargar la información inicial.", ex.Message);
        }
    }

    private bool TryLoadPersistedTokens()
    {
        if (ApplicationState.TryTakeFromJson<AntiforgeryPayload>(AntiforgeryStateKey, out var payload)
            && payload is not null
            && !string.IsNullOrEmpty(payload.FieldName)
            && !string.IsNullOrEmpty(payload.Token))
        {
            SetAntiforgeryValues(payload.FieldName, payload.Token);
            return true;
        }

        return false;
    }

    private void GenerateTokensFromHttpContext()
    {
        var httpContext = HttpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            _canSubmitLogout = false;
            return;
        }

        var tokens = Antiforgery.GetAndStoreTokens(httpContext);
        SetAntiforgeryValues(tokens.FormFieldName, tokens.RequestToken);
        _canSubmitLogout = true;
    }

    private Task PersistAntiforgeryTokensAsync()
    {
        if (string.IsNullOrEmpty(_antiforgeryFieldName) || string.IsNullOrEmpty(_antiforgeryToken))
        {
            GenerateTokensFromHttpContext();
        }

        if (!string.IsNullOrEmpty(_antiforgeryFieldName) && !string.IsNullOrEmpty(_antiforgeryToken))
        {
            ApplicationState.PersistAsJson(AntiforgeryStateKey, new AntiforgeryPayload(_antiforgeryFieldName, _antiforgeryToken));
            _canSubmitLogout = true;
        }
        else
        {
            _canSubmitLogout = false;
        }

        return Task.CompletedTask;
    }

    private void SetAntiforgeryValues(string? fieldName, string? token)
    {
        _antiforgeryFieldName = fieldName;
        _antiforgeryToken = token;
        _canSubmitLogout = !string.IsNullOrEmpty(_antiforgeryFieldName) && !string.IsNullOrEmpty(_antiforgeryToken);
    }

    private void ToggleAppSwitcher() => _appSwitcherOpen = !_appSwitcherOpen;

    private static string GetAppDisplayName(ApplicationClientAdminDto client)
        => !string.IsNullOrWhiteSpace(client.Audience)
            ? client.Audience
            : client.ClientId;

    private static string GetDisplayName(ApplicationUser? user)
    {
        if (user is null)
        {
            return string.Empty;
        }

        var displayName = !string.IsNullOrWhiteSpace(user.Nombre)
            ? user.Nombre
            : user.UserName;

        if (!string.IsNullOrWhiteSpace(displayName) && displayName.Contains('@'))
        {
            displayName = displayName.Split('@')[0];
        }

        return displayName ?? string.Empty;
    }

    public void Dispose()
        => _subscription?.Dispose();

    private sealed record AntiforgeryPayload(string FieldName, string Token);
}
