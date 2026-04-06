namespace Auth.Web.Data.Entities;

public class UserPageOverride
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public int? PageId { get; set; }

    public int? ActionPermissionId { get; set; }

    /// <summary>GRANT o DENY</summary>
    public string Type { get; set; } = string.Empty;

    public Page? Page { get; set; }

    public ActionPermission? ActionPermission { get; set; }
}
