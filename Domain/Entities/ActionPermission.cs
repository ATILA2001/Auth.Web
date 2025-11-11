namespace Auth.Web.Domain.Entities;

public class ActionPermission
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<RolePagePermission> RolePermissions { get; set; } = new List<RolePagePermission>();
}
