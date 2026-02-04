namespace Auth.Web.Application.Admin.Dtos;

public sealed class AreaRouteAdminDto
{
    public int Id { get; set; }
    public int? AreaId { get; set; }
    public int? ClientId { get; set; } // NOTE: maps to ApplicationClient.Id
    public string ClientIdentifier { get; set; } = string.Empty; // underlying ClientId string value
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    public string? AreaName { get; set; }
    public string? ApplicationName { get; set; }
}
