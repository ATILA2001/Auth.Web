namespace Auth.Web.Application.Admin.Dtos;

public sealed class PageAdminDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int PermissionCount { get; set; }
}
