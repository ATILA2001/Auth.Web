namespace Auth.Web.Data.Entities;

public class AreaPagePermission
{
    public int Id { get; set; }

    public int AreaId { get; set; }

    public int? PageId { get; set; }

    public int? ActionPermissionId { get; set; }

    public Area? Area { get; set; }

    public Page? Page { get; set; }

    public ActionPermission? ActionPermission { get; set; }
}
