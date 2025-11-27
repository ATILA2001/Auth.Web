using System.ComponentModel.DataAnnotations;

namespace Auth.Web.Components.Account.Pages;

public sealed class LoginInputModel
{
    [Required(ErrorMessage = "Debe ingresar su correo electrónico."), EmailAddress (ErrorMessage = "El correo electrónico no es valido.")]
    public string UserNameOrEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe ingresar su contraseña.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}
