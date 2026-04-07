using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Authentication;
using Auth.Web.Services.Abstractions.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components.Infrastructure;

namespace Auth.Web.Components.Account.Pages;

public partial class Login : ComponentBase, IDisposable
{
    private const int BackgroundImageCount = 23;
    private const string AntiforgeryStateKey = "login-antiforgery";

    [Inject] private IHttpContextAccessor HttpContextAccessor { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private IUserRegistrationService UserRegistrationService { get; set; } = null!;
    [Inject] private IAntiforgery Antiforgery { get; set; } = null!;
    [Inject] private PersistentComponentState ApplicationState { get; set; } = null!;

    private LoginViewModel _vm = null!;
    private bool IsFlipped { get; set; }
    private string BackgroundStyle { get; set; } = string.Empty;
    private string? _antiforgeryFieldName;
    private string? _antiforgeryToken;
    private PersistingComponentStateSubscription? _subscription;

    private string loginUser
    {
        get => _vm.LoginUser;
        set => _vm.LoginUser = value;
    }

    public string? ErrorMessage => _vm.ErrorMessage;
    public string? ReturnUrlFromQuery => _vm.ReturnUrl;
    public string? ClientIdFromQuery => _vm.ClientId;
    public LoginViewModel.RegisterInputModel RegInput => _vm.Register;
    public string? RegisterMessage => _vm.RegisterMessage;
    public string? AntiforgeryFieldName => _antiforgeryFieldName;
    public string? AntiforgeryToken => _antiforgeryToken;

    protected override void OnInitialized()
    {
        _vm = new LoginViewModel(UserRegistrationService);
        _vm.LoadFromQuery(NavigationManager.ToAbsoluteUri(NavigationManager.Uri));

        var imageIndex = Random.Shared.Next(1, BackgroundImageCount + 1);
        BackgroundStyle = $"background-image: linear-gradient(rgba(0, 0, 0, 0.4), rgba(0, 0, 0, 0.4)), url('images/{imageIndex}-IVC.jpg');";

        _subscription = ApplicationState.RegisterOnPersisting(PersistAntiforgeryTokenAsync);

        if (!TryLoadPersistedTokens())
        {
            GenerateTokensFromHttpContext();
        }
    }

    protected override async Task OnInitializedAsync()
    {
        var httpContext = HttpContextAccessor.HttpContext;
        if (httpContext is not null && HttpMethods.IsGet(httpContext.Request.Method))
        {
            await httpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        }
    }

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

    private void FlipCard() => IsFlipped = !IsFlipped;

    public async Task RegisterUser()
    {
        await _vm.RegisterUserAsync();

        if (string.IsNullOrEmpty(_vm.RegisterMessage))
        {
            await Task.Delay(500);
            IsFlipped = false;
        }

        StateHasChanged();
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }

    private sealed record AntiforgeryPayload(string FieldName, string Token);
}
