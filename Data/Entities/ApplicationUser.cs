using Microsoft.AspNetCore.Identity;

namespace Auth.Web.Data.Entities;

public class ApplicationUser : IdentityUser
{
    public string? Nombre { get; set; }

    public int PermissionVersion { get; set; } = 1;
}
