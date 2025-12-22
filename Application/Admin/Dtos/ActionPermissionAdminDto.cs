namespace Auth.Web.Application.Admin.Dtos;

public sealed class ActionPermissionAdminDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int UsageCount { get; set; }
}
