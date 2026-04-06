namespace Auth.Web.Application.Permissions.Dtos;

public class UserPermissionsDto
{
    public List<PagePermissionDto> Pages { get; set; } = new();

    public List<int> AreaIds { get; set; } = new();

    public int Version { get; set; }
}
