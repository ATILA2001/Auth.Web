using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Auth.Web.Application.Admin.Dtos;
using Radzen;

namespace Auth.Web.Components.Admin;

public partial class AdminLayout : LayoutComponentBase, IDisposable
{
    private const string AntiforgeryStateKey = "admin-logout-antiforgery";

    bool sidebarExpanded = true;

    private List<ApplicationClientAdminDto> clients = new();
    private int? selectedClientId;
    private string? CurrentUserName = string.Empty;
    private string? _antiforgeryFieldName;
    private string? _antiforgeryToken;
    private bool _canSubmitLogout;
    private PersistingComponentStateSubscription? _subscription;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

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
            CurrentUserName = authState.User.Identity?.Name ?? string.Empty;

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

    private void OnClientChanged(object? value)
    {
        var id = value as int?;
        var client = clients.FirstOrDefault(c => c.Id == id);
        var url = client?.AllowedReturnUrls?.FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(url))
        {
            NavigationManager.NavigateTo(url!, true);
        }
    }

    public void Dispose()
        => _subscription?.Dispose();

    private sealed record AntiforgeryPayload(string FieldName, string Token);
}
