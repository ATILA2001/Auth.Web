using Auth.Web.Application.Abstractions;
using Auth.Web.Application.Dtos;
using Auth.Web.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Auth.Web.Application.Users;

public sealed class UserRegistrationService : IUserRegistrationService
{
    private readonly IActiveDirectoryAuthService _adAuth;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserStore<ApplicationUser> _userStore;
    private readonly ILogger<UserRegistrationService> _logger;

    public UserRegistrationService(
        IActiveDirectoryAuthService adAuth,
        UserManager<ApplicationUser> userManager,
        IUserStore<ApplicationUser> userStore,
        ILogger<UserRegistrationService> logger)
    {
        _adAuth = adAuth;
        _userManager = userManager;
        _userStore = userStore;
        _logger = logger;
    }

    public async Task<RegisterUserResult> RegisterUserAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email?.Trim() ?? string.Empty;
        var fullName = request.FullName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(fullName))
        {
            return RegisterUserResult.ValidationError("Complete los campos requeridos.");
        }

        var existing = await _userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            return RegisterUserResult.AlreadyExists("El correo ya está registrado.");
        }

        var existsInAd = await _adAuth.ExistsByEmailAsync(email);
        if (!existsInAd)
        {
            return RegisterUserResult.NotInActiveDirectory("El correo no pertenece al dominio (AD) o no existe en el directorio.");
        }

        var user = new ApplicationUser { Nombre = fullName };
        await _userStore.SetUserNameAsync(user, email, cancellationToken);
        var emailStore = GetEmailStore();
        await emailStore.SetEmailAsync(user, email, cancellationToken);

        var result = await _userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            return RegisterUserResult.ValidationError(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        _logger.LogInformation("Usuario creado sin password local (AD-backed).");
        return RegisterUserResult.Success("Cuenta creada correctamente. Inicie sesión.");
    }

    private IUserEmailStore<ApplicationUser> GetEmailStore()
    {
        if (!_userManager.SupportsUserEmail)
        {
            throw new NotSupportedException("Se requiere un UserStore con soporte de email.");
        }
        return (IUserEmailStore<ApplicationUser>)_userStore;
    }
}