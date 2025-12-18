using System.ComponentModel.DataAnnotations;
using Auth.Web.Application.Dtos;
using Auth.Web.Services.Abstractions.Users;
using Microsoft.AspNetCore.WebUtilities;

namespace Auth.Web.Components.Account.Pages;

public sealed class LoginViewModel
{
    private readonly IUserRegistrationService _registrationService;

    public LoginViewModel(IUserRegistrationService registrationService)
    {
        ArgumentNullException.ThrowIfNull(registrationService);
        _registrationService = registrationService;
    }

    public int SelectedTabIndex { get; set; }
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

        ErrorMessage = null;
        ReturnUrl = null;
        ClientId = null;

        var query = QueryHelpers.ParseQuery(uri.Query);

        if (query.TryGetValue("error", out var errorValues))
            ErrorMessage = errorValues.FirstOrDefault();

        if (query.TryGetValue("returnUrl", out var returnValues))
            ReturnUrl = returnValues.FirstOrDefault();

        if (query.TryGetValue("clientId", out var clientValues))
            ClientId = clientValues.FirstOrDefault();
    }

    public async Task RegisterUserAsync()
    {
        RegisterMessage = null;
        SuccessMessage = null;

        var fullName = (Register.FullName ?? string.Empty).Trim();
        var email = (Register.Email ?? string.Empty).Trim();

        var request = new RegisterUserRequest
        {
            FullName = fullName,
            Email = email
        };

        var result = await _registrationService.RegisterUserAsync(request);

        switch (result.Type)
        {
            case RegisterUserResultType.Success:
                SuccessMessage = result.Message;
                LoginUser = email;
                SelectedTabIndex = 0;
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
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe ingresar correo electrónico.")]
        [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
        public string Email { get; set; } = string.Empty;
    }
}
