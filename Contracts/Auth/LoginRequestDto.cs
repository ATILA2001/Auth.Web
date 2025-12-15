using System.ComponentModel.DataAnnotations;

namespace Auth.Web.Contracts.Auth;

public sealed class LoginRequestDto
{
    [Required]
    public string UserNameOrEmail { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
    public string? ClientId { get; set; }
}
