using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Authentication;
using Auth.Web.Services.Abstractions.Users;
using Auth.Web.Application.Dtos;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Auth.Web.Components.Account.Pages;

public partial class Login : ComponentBase
{
    private string? errorMessage;
    private int selectedTabIndex = 0;

    [Inject] private IHttpContextAccessor HttpContextAccessor { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IUserRegistrationService UserRegistrationService { get; set; } = default!;

    public LoginInputModel Input { get; set; } = new();
    public string? ErrorMessage => errorMessage;

    public RegisterInputModel RegInput { get; set; } = new();
    public string? RegisterMessage { get; set; }
    public string? SuccessMessage { get; set; }

    protected override void OnInitialized()
    {
        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        var query = QueryHelpers.ParseQuery(uri.Query);

        if (query.TryGetValue("error", out var errorValues))
            errorMessage = errorValues.FirstOrDefault() ?? string.Empty;

        if (query.TryGetValue("returnUrl", out var r))
            ReturnUrlFromQuery = r.FirstOrDefault() ?? string.Empty;

        if (query.TryGetValue("clientId", out var c))
            ClientIdFromQuery = c.FirstOrDefault() ?? string.Empty;
    }

    protected override async Task OnInitializedAsync()
    {
        var httpContext = HttpContextAccessor.HttpContext;
        if (httpContext != null && HttpMethods.IsGet(httpContext.Request.Method))
        {
            await httpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        }
    }

    public string? ReturnUrlFromQuery { get; set; }
    public string? ClientIdFromQuery { get; set; }

    public async Task RegisterUser()
    {
        RegisterMessage = null;
        SuccessMessage = null;

        var request = new RegisterUserRequest
        {
            FullName = RegInput.FullName,
            Email = RegInput.Email
        };

        var result = await UserRegistrationService.RegisterUserAsync(request);

        switch (result.Type)
        {
            case RegisterUserResultType.Success:
                SuccessMessage = result.Message;
                Input.UserNameOrEmail = RegInput.Email;
                selectedTabIndex = 0;
                StateHasChanged();
                break;
            case RegisterUserResultType.AlreadyExists:
            case RegisterUserResultType.NotInActiveDirectory:
            case RegisterUserResultType.ValidationError:
                RegisterMessage = result.Message;
                break;
        }
    }

    public sealed class RegisterInputModel
    {
        [Required (ErrorMessage ="Debe ingresar nombre completo.")]
        public string FullName { get; set; } = string.Empty;
        [Required (ErrorMessage = "Debe ingresar correo electronico."), EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
