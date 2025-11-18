using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms; // EditContext
using Microsoft.AspNetCore.Identity;
using Auth.Web.Data;
using Auth.Web.Domain.Entities; // ApplicationUser
using Auth.Web.Services.Abstractions;
using Auth.Web.Components.Account; // IdentityRedirectManager

namespace Auth.Web.Components.Account.Pages;

public partial class Register : ComponentBase
{
    private IEnumerable<IdentityError>? identityErrors;

    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = new();

    [SupplyParameterFromQuery]
    private string? ReturnUrl { get; set; }

    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private IUserStore<ApplicationUser> UserStore { get; set; } = default!;
    [Inject] private SignInManager<ApplicationUser> SignInManager { get; set; } = default!; // kept in case of future use
    [Inject] private ILogger<Register> Logger { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IdentityRedirectManager RedirectManager { get; set; } = default!;
    [Inject] private IAdAuthService AdAuth { get; set; } = default!;

    private string? Message => identityErrors is null ? null : $"Error: {string.Join(", ", identityErrors.Select(error => error.Description))}";

    public async Task RegisterUser(EditContext editContext)
    {
        identityErrors = null;

        var email = Input.Email.Trim();
        var fullName = Input.FullName.Trim();

        var existing = await UserManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            identityErrors = new[] { new IdentityError { Description = "El correo ya está registrado." } };
            return;
        }

        var existsInAd = await AdAuth.ExistsByEmailAsync(email);
        if (!existsInAd)
        {
            identityErrors = new[] { new IdentityError { Description = "El correo no pertenece al dominio (AD) o no existe en el directorio." } };
            return;
        }

        var user = CreateUser();
        user.Nombre = fullName;

        await UserStore.SetUserNameAsync(user, email, CancellationToken.None);
        var emailStore = GetEmailStore();
        await emailStore.SetEmailAsync(user, email, CancellationToken.None);

        var result = await UserManager.CreateAsync(user);

        if (!result.Succeeded)
        {
            identityErrors = result.Errors;
            return;
        }

        Logger.LogInformation("User created a new account without local password (AD-backed).\n");
        RedirectManager.RedirectTo("Account/Login");
    }

    private ApplicationUser CreateUser()
    {
        try
        {
            return Activator.CreateInstance<ApplicationUser>();
        }
        catch
        {
            throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. Ensure it has a parameterless constructor.");
        }
    }

    private IUserEmailStore<ApplicationUser> GetEmailStore()
    {
        if (!UserManager.SupportsUserEmail)
        {
            throw new NotSupportedException("The default UI requires a user store with email support.");
        }
        return (IUserEmailStore<ApplicationUser>)UserStore;
    }

    private sealed class InputModel
    {
        [Required]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;
    }
}
