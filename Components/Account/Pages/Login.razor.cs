using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Auth.Web.Services.Abstractions;
using Microsoft.Extensions.Logging;
using Auth.Web.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace Auth.Web.Components.Account.Pages;

public partial class Login : ComponentBase
{
    private string? errorMessage;
    private int selectedTabIndex = 0;

    [Inject] private IHttpContextAccessor HttpContextAccessor { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private ILoginService LoginService { get; set; } = default!;
    [Inject] private ILogger<Login> Logger { get; set; } = default!;

    // Registro deps
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private IUserStore<ApplicationUser> UserStore { get; set; } = default!;
    [Inject] private IAdAuthService AdAuth { get; set; } = default!;

    public LoginInputModel Input { get; set; } = new();
    public string? ErrorMessage => errorMessage;

    // Registro state
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
        // Prellenar login y cambiar a tab de login
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
