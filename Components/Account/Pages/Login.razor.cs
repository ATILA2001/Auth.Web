using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Auth.Web.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.WebUtilities;

namespace Auth.Web.Components.Account.Pages;

public partial class Login : ComponentBase
{
    private string? errorMessage;
    private int selectedTabIndex = 0;

    [Inject] private IHttpContextAccessor HttpContextAccessor { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private ILogger<Login> Logger { get; set; } = default!;

    // Registro deps para signup manual
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private IUserStore<ApplicationUser> UserStore { get; set; } = default!;
    [Inject] private Auth.Web.Services.Abstractions.IAdAuthService AdAuth { get; set; } = default!;

    public LoginInputModel Input { get; set; } = new();
    public string? ErrorMessage => errorMessage;

    public RegisterInputModel RegInput { get; set; } = new();
    public string? RegisterMessage { get; set; }
    public string? SuccessMessage { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var httpContext = HttpContextAccessor.HttpContext;
        if (httpContext != null && HttpMethods.IsGet(httpContext.Request.Method))
        {
            await httpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        }

        var uri = new Uri(NavigationManager.Uri);
        var query = QueryHelpers.ParseQuery(uri.Query);
        if (query.TryGetValue("returnUrl", out var r)) ReturnUrlFromQuery = r.ToString();
        if (query.TryGetValue("clientId", out var c)) ClientIdFromQuery = c.ToString();
    }

    public string? ReturnUrlFromQuery { get; set; }
    public string? ClientIdFromQuery { get; set; }

    public async Task RegisterUser()
    {
        RegisterMessage = null;
        SuccessMessage = null;

        var email = RegInput.Email?.Trim() ?? string.Empty;
        var fullName = RegInput.FullName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(fullName))
        {
            RegisterMessage = "Complete los campos requeridos.";
            return;
        }

        var existing = await UserManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            RegisterMessage = "El correo ya está registrado.";
            return;
        }

        var existsInAd = await AdAuth.ExistsByEmailAsync(email);
        if (!existsInAd)
        {
            RegisterMessage = "El correo no pertenece al dominio (AD) o no existe en el directorio.";
            return;
        }

        var user = new ApplicationUser { Nombre = fullName };
        await UserStore.SetUserNameAsync(user, email, CancellationToken.None);
        var emailStore = GetEmailStore();
        await emailStore.SetEmailAsync(user, email, CancellationToken.None);

        var result = await UserManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            RegisterMessage = string.Join(", ", result.Errors.Select(e => e.Description));
            return;
        }

        Logger.LogInformation("Usuario creado sin password local (AD-backed).");
        SuccessMessage = "Cuenta creada correctamente. Inicie sesión.";
        Input.UserNameOrEmail = email;
        selectedTabIndex = 0;
        StateHasChanged();
    }

    private IUserEmailStore<ApplicationUser> GetEmailStore()
    {
        if (!UserManager.SupportsUserEmail)
        {
            throw new NotSupportedException("Se requiere un UserStore con soporte de email.");
        }
        return (IUserEmailStore<ApplicationUser>)UserStore;
    }

    public sealed class RegisterInputModel
    {
        [Required]
        public string FullName { get; set; } = string.Empty;
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
