namespace Auth.Web.Application.Admin.Dtos;

public sealed class AreaAdminDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int UserCount { get; set; }
    public int? ClientId { get; set; }
    public string? ClientName { get; set; }
}
