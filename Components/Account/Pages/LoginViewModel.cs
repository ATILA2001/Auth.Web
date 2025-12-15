using System.ComponentModel.DataAnnotations;

namespace Auth.Web.Components.Account.Pages;

public sealed class LoginViewModel
{
    [Required]
    public string UserNameOrEmail { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrlFromQuery { get; set; }
    public string? ClientIdFromQuery { get; set; }

    public string? ErrorMessage { get; set; }
    public string? RegisterMessage { get; set; }

    public int SelectedTabIndex { get; set; } = 0;

    // Register UI-only models (if not used by controllers)
    public sealed class RegisterInput
    {
        [Required]
        public string FullName { get; set; } = string.Empty;
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public RegisterInput RegInput { get; set; } = new();
}
