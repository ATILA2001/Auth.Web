namespace Auth.Web.Domain.Dtos;

public class PagePermissionDto
{
    public string Url { get; set; } = string.Empty;

    public List<string> Actions { get; set; } = new();
}
