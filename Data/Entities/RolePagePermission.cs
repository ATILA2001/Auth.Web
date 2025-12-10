namespace Auth.Web.Data.Entities;

public class RolePagePermission
{
    public int Id { get; set; }

    public string RoleId { get; set; } = string.Empty;

    public int PageId { get; set; }

    public int ActionPermissionId { get; set; }

    public Page? Page { get; set; }

    public ActionPermission? ActionPermission { get; set; }
}
