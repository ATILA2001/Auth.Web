using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Authentication;
using Auth.Web.Services.Abstractions.Users;
using Microsoft.AspNetCore.Identity;

namespace Auth.Web.Components.Account.Pages;

public partial class Login : ComponentBase
{
    private const int BackgroundImageCount = 23;
    
    [Inject] private IHttpContextAccessor HttpContextAccessor { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private IUserRegistrationService UserRegistrationService { get; set; } = null!;

    private LoginViewModel _vm = null!;
    private bool IsFlipped { get; set; }
    private string BackgroundStyle { get; set; } = string.Empty;

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

    protected override void OnInitialized()
    {
        _vm = new LoginViewModel(UserRegistrationService);
        _vm.LoadFromQuery(NavigationManager.ToAbsoluteUri(NavigationManager.Uri));
        
        var imageIndex = Random.Shared.Next(1, BackgroundImageCount + 1);
        BackgroundStyle = $"background-image: linear-gradient(rgba(0, 0, 0, 0.4), rgba(0, 0, 0, 0.4)), url('images/{imageIndex}-IVC.jpg');";
    }

    protected override async Task OnInitializedAsync()
    {
        var httpContext = HttpContextAccessor.HttpContext;
        if (httpContext is not null && HttpMethods.IsGet(httpContext.Request.Method))
        {
            await httpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        }
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
}
