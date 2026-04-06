namespace Auth.Web.Data.Entities;

public class Page
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public int? ClientId { get; set; }

    public ApplicationClient? Client { get; set; }

    public ICollection<RolePagePermission> RolePermissions { get; set; } = new List<RolePagePermission>();

    public ICollection<AreaPagePermission> AreaPermissions { get; set; } = new List<AreaPagePermission>();
}
