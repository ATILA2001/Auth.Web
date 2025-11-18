using System.ComponentModel.DataAnnotations;

namespace Auth.Web.Components.Account.Pages;

public sealed class LoginInputModel
{
    [Required]
    public string UserNameOrEmail { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}
