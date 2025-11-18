using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Auth.Web.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace Auth.Web.Components.Account.Pages;

public partial class Login : ComponentBase
{
    private string? errorMessage;

    [Inject] private IHttpContextAccessor HttpContextAccessor { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private ILoginService LoginService { get; set; } = default!;
    [Inject] private ILogger<Login> Logger { get; set; } = default!;

    public LoginInputModel Input { get; set; } = new();
    public string? ErrorMessage => errorMessage;

    protected override async Task OnInitializedAsync()
    {
        var httpContext = HttpContextAccessor.HttpContext;
        if (httpContext != null && HttpMethods.IsGet(httpContext.Request.Method))
        {
            await httpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        }
    }

    public async Task ExecuteLogin()
    {
        var model = Input;
        Logger.LogInformation("Intentando login para {User}", model.UserNameOrEmail);

        var result = await LoginService.LoginAsync(model.UserNameOrEmail.Trim(), model.Password);
        if (!result.Success)
        {
            errorMessage = result.Error;
            Logger.LogWarning("Fallo login para {User}: {Error}", model.UserNameOrEmail, result.Error);
            return;
        }

        var target = string.IsNullOrWhiteSpace(result.Redirect) ? "/admin" : result.Redirect!;
        Logger.LogInformation("Login exitoso para {User}, navegando a {Target}", model.UserNameOrEmail, target);
        NavigationManager.NavigateTo(target, forceLoad: true);
    }
}
