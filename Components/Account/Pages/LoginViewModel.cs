using System.ComponentModel.DataAnnotations;
using Auth.Web.Application.Users.Registration;
using Auth.Web.Services.Abstractions.Users;
using Microsoft.AspNetCore.WebUtilities;

namespace Auth.Web.Components.Account.Pages;

public sealed class LoginViewModel
{
    private readonly IUserRegistrationService _registrationService;

    public LoginViewModel(IUserRegistrationService registrationService)
    {
        _registrationService = registrationService ?? throw new ArgumentNullException(nameof(registrationService));
    }

    public string? ErrorMessage { get; private set; }
    public string? ReturnUrl { get; private set; }
    public string? ClientId { get; private set; }
    public string LoginUser { get; set; } = string.Empty;
    public RegisterInputModel Register { get; } = new();
    public string? RegisterMessage { get; private set; }
    public string? SuccessMessage { get; private set; }

    public void LoadFromQuery(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        var query = QueryHelpers.ParseQuery(uri.Query);

        var errorCode = query.TryGetValue("errorCode", out var errorCodeValues)
            ? errorCodeValues.FirstOrDefault()
            : null;
        var fallbackError = query.TryGetValue("error", out var errorValues)
            ? errorValues.FirstOrDefault()
            : null;
        var unlockAt = TryParseUnlockAt(query);
        var requiresAdminUnlock = query.TryGetValue("requiresAdminUnlock", out var adminUnlockValues)
            && bool.TryParse(adminUnlockValues.FirstOrDefault(), out var parsedAdminUnlock)
            && parsedAdminUnlock;

        ErrorMessage = BuildErrorMessage(errorCode, fallbackError, unlockAt, requiresAdminUnlock);
        ReturnUrl = query.TryGetValue("returnUrl", out var returnValues)
            ? returnValues.FirstOrDefault() : null;
        ClientId = query.TryGetValue("clientId", out var clientValues) 
            ? clientValues.FirstOrDefault() : null;
    }

    private static DateTimeOffset? TryParseUnlockAt(Dictionary<string, Microsoft.Extensions.Primitives.StringValues> query)
    {
        if (!query.TryGetValue("unlockAt", out var values))
        {
            return null;
        }

        return DateTimeOffset.TryParse(
            values.FirstOrDefault(),
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.RoundtripKind,
            out var unlockAt)
                ? unlockAt
                : null;
    }

    private static string? BuildErrorMessage(
        string? errorCode,
        string? fallbackError,
        DateTimeOffset? unlockAtUtc,
        bool requiresAdminUnlock)
    {
        return errorCode?.Trim().ToLowerInvariant() switch
        {
            "invalid_credentials" => "Usuario o contraseña inválidos.",
            "account_locked" when requiresAdminUnlock =>
                "La cuenta de dominio está bloqueada. Contacte al administrador para desbloquearla.",
            "account_locked" when unlockAtUtc.HasValue =>
                $"La cuenta de dominio está bloqueada. Podrá volver a intentar después del {unlockAtUtc.Value.ToLocalTime():dd/MM/yyyy HH:mm}.",
            "account_locked" =>
                "La cuenta de dominio está bloqueada. Intente nuevamente más tarde o contacte al administrador.",
            "ad_unavailable" =>
                "El servicio de autenticación de dominio no está disponible. Intente nuevamente más tarde.",
            "ad_error" =>
                "No se pudo validar la cuenta de dominio. Intente nuevamente más tarde.",
            _ => fallbackError
        };
    }
    public async Task RegisterUserAsync()
    {
        RegisterMessage = null;
        SuccessMessage = null;

        var request = new RegisterUserRequest
        {
            Cuil = (Register.Cuil ?? string.Empty).Trim(),
            Email = (Register.Email ?? string.Empty).Trim()
        };

        var result = await _registrationService.RegisterUserAsync(request);

        switch (result.Type)
        {
            case RegisterUserResultType.Success:
                SuccessMessage = result.Message;
                LoginUser = request.Email;
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
        [Required(ErrorMessage = "Debe ingresar el CUIL.")]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "El CUIL debe contener exactamente 11 dígitos numéricos.")]
        public string Cuil { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe ingresar correo electrónico.")]
        [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
        [MaxLength(254, ErrorMessage = "El correo electrónico no puede superar 254 caracteres.")]
        public string Email { get; set; } = string.Empty;
    }
}
