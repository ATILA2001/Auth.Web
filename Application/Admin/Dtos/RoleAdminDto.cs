namespace Auth.Web.Application.Admin.Dtos;

public sealed class RoleAdminDto
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int UserCount { get; set; }
}
