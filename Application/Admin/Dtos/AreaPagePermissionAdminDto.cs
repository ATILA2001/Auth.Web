namespace Auth.Web.Application.Admin.Dtos;

public sealed class AreaPagePermissionAdminDto
{
    public int Id { get; set; }
    public int AreaId { get; set; }
    public string AreaName { get; set; } = string.Empty;
    public int? PageId { get; set; }
    public string PageName { get; set; } = "Sin asignar";
    public string PageUrl { get; set; } = string.Empty;
    public int? ActionPermissionId { get; set; }
    public string ActionName { get; set; } = "Sin asignar";
}
