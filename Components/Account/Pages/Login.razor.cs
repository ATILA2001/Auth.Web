using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Authentication;
using Auth.Web.Services.Abstractions.Users;
using Microsoft.AspNetCore.Identity;

namespace Auth.Web.Components.Account.Pages;

public partial class Login : ComponentBase
{
    [Inject] private IHttpContextAccessor HttpContextAccessor { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private IUserRegistrationService UserRegistrationService { get; set; } = null!;

    private LoginViewModel _vm = null!;

    private int selectedTabIndex
    {
        get => _vm.SelectedTabIndex;
        set => _vm.SelectedTabIndex = value;
    }

    private string loginUser
    {
        get => _vm.LoginUser;
        set => _vm.LoginUser = value;
    }

    public string? ErrorMessage => _vm.ErrorMessage;
    public string? SuccessMessage => _vm.SuccessMessage;
    public string? ReturnUrlFromQuery => _vm.ReturnUrl;
    public string? ClientIdFromQuery => _vm.ClientId;

    public LoginViewModel.RegisterInputModel RegInput => _vm.Register;
    public string? RegisterMessage => _vm.RegisterMessage;

    protected override void OnInitialized()
    {
        _vm = new LoginViewModel(UserRegistrationService);
        _vm.LoadFromQuery(NavigationManager.ToAbsoluteUri(NavigationManager.Uri));
    }

    protected override async Task OnInitializedAsync()
    {
        var httpContext = HttpContextAccessor.HttpContext;
        if (httpContext != null && HttpMethods.IsGet(httpContext.Request.Method))
        {
            await httpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        }
    }

    public async Task RegisterUser()
    {
        await _vm.RegisterUserAsync();
        StateHasChanged();
    }
}
