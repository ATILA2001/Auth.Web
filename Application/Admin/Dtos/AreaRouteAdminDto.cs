namespace Auth.Web.Application.Admin.Dtos;

public sealed class AreaRouteAdminDto
{
    public int Id { get; init; }
    public int AreaId { get; init; }
    public int ClientId { get; init; } // NOTE: maps to ApplicationClient.Id, ClientId string also available via ApplicationName
    public string ClientIdentifier { get; init; } = string.Empty; // underlying ClientId string value
    public string ReturnUrl { get; init; } = string.Empty;
    public int Priority { get; init; }
    public bool IsActive { get; init; }
    public string? AreaName { get; init; }
    public string? ApplicationName { get; init; }
}
