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

        ErrorMessage = query.TryGetValue("error", out var errorValues) 
            ? errorValues.FirstOrDefault() : null;
        ReturnUrl = query.TryGetValue("returnUrl", out var returnValues) 
            ? returnValues.FirstOrDefault() : null;
        ClientId = query.TryGetValue("clientId", out var clientValues) 
            ? clientValues.FirstOrDefault() : null;
    }

    public async Task RegisterUserAsync()
    {
        RegisterMessage = null;
        SuccessMessage = null;

        var request = new RegisterUserRequest
        {
            FullName = (Register.FullName ?? string.Empty).Trim(),
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
        [Required(ErrorMessage = "Debe ingresar nombre completo.")]
        [MaxLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe ingresar correo electrónico.")]
        [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
        [MaxLength(254, ErrorMessage = "El correo electrónico no puede superar 254 caracteres.")]
        public string Email { get; set; } = string.Empty;
    }
}
