namespace Auth.Web.Application.Admin.Dtos;

public sealed class RoleAdminDto
{
    public string Id { get; init; } = default!;
    public string Name { get; init; } = default!;
    public int UserCount { get; init; }
}
