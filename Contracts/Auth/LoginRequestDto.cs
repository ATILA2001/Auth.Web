using System.ComponentModel.DataAnnotations;

namespace Auth.Web.Contracts.Auth;

public sealed class LoginRequestDto
{
    [Required]
    [MaxLength(100)]
    public string UserNameOrEmail { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string Password { get; set; } = string.Empty;

    [MaxLength(2048)]
    public string? ReturnUrl { get; set; }

    [MaxLength(50)]
    public string? ClientId { get; set; }
}
