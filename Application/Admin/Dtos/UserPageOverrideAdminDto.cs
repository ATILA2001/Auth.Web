namespace Auth.Web.Application.Admin.Dtos;

public sealed class UserPageOverrideAdminDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int? PageId { get; set; }
    public string PageName { get; set; } = "Sin asignar";
    public string PageUrl { get; set; } = string.Empty;
    public int? ActionPermissionId { get; set; }
    public string ActionName { get; set; } = "Sin asignar";

    /// <summary>True permite; false deniega.</summary>
    public bool IsAllowed { get; set; }
}
