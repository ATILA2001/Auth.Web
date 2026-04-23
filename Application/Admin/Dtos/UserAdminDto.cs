namespace Auth.Web.Application.Admin.Dtos;

public sealed class UserAdminDto
{
    public string Id { get; set; } = default!;
    public string UserName { get; set; } = default!;
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public IReadOnlyCollection<string> Roles { get; set; } = Array.Empty<string>();
    public IReadOnlyCollection<string> Areas { get; set; } = Array.Empty<string>();
    public IReadOnlyCollection<int> AreaIds { get; set; } = Array.Empty<int>();
    public IReadOnlyCollection<string> ClientIds { get; set; } = Array.Empty<string>();
    public int PermissionVersion { get; set; }
}
