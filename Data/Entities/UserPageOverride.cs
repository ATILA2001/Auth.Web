namespace Auth.Web.Data.Entities;

public class UserPageOverride
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public int? PageId { get; set; }

    public int? ActionPermissionId { get; set; }

    /// <summary>True permite; false deniega.</summary>
    public bool IsAllowed { get; set; }

    public Page? Page { get; set; }

    public ActionPermission? ActionPermission { get; set; }
}
