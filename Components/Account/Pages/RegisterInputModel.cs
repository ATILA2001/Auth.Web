using System.ComponentModel.DataAnnotations;

namespace Auth.Web.Components.Account.Pages;

public sealed class RegisterInputModel
{
    [Required(ErrorMessage = "Debe ingresar nombre completo.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe ingresar correo electrónico."), EmailAddress]
    public string Email { get; set; } = string.Empty;
}