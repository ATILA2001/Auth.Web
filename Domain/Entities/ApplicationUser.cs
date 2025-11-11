using Microsoft.AspNetCore.Identity;

namespace Auth.Web.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string? Nombre { get; set; }

    public string? Cuil { get; set; }
}
