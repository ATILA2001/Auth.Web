namespace Auth.Web.Domain.Entities;

public class ApplicationClient
{
    public int Id { get; set; }

    public string ClientId { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string AllowedReturnUrlsJson { get; set; } = string.Empty;
}
