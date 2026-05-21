namespace Auth.Web.Application.Admin.Dtos;

public sealed class ApplicationClientAdminDto
{
    public int Id { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public IReadOnlyCollection<string> AllowedReturnUrls { get; set; } = Array.Empty<string>();

    public string? DefaultLandingPage { get; set; }
}
