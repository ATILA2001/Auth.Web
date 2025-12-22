namespace Auth.Web.Application.Admin.Dtos;

public sealed class RolePagePermissionAdminDto
{
    public int Id { get; set; }
    public string RoleId { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public int PageId { get; set; }
    public string PageName { get; set; } = string.Empty;
    public string PageUrl { get; set; } = string.Empty;
    public int ActionPermissionId { get; set; }
    public string ActionName { get; set; } = string.Empty;
}
