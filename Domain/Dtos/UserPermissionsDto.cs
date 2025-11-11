namespace Auth.Web.Domain.Dtos;

public class UserPermissionsDto
{
    public List<PagePermissionDto> Pages { get; set; } = new();

    public List<int> Areas { get; set; } = new();

    public int Version { get; set; }
}
