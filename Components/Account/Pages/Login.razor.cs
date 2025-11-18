using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Auth.Web.Services.Abstractions;

namespace Auth.Web.Components.Account.Pages;

public partial class Login : ComponentBase
{
    private string? errorMessage;

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = new();

    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private ILoginService LoginService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        if (HttpMethods.IsGet(HttpContext.Request.Method))
        {
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        }
    }

    public async Task LoginUser()
    {
        errorMessage = null;

        var login = Input.UserNameOrEmail.Trim();
        var password = Input.Password;

        var result = await LoginService.LoginAsync(login, password);
        if (!result.Success)
        {
            errorMessage = result.Error;
            return;
        }

        if (!string.IsNullOrWhiteSpace(result.Redirect))
        {
            NavigationManager.NavigateTo(result.Redirect!, forceLoad: true);
        }
        else
        {
            NavigationManager.NavigateTo("/account/dashboard");
        }
    }

    private sealed class InputModel
    {
        [Required]
        public string UserNameOrEmail { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}
