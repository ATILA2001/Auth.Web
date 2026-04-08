namespace Auth.Web.Application.Permissions.Dtos;

public class PagePermissionDto
{
    public int PageId { get; set; }

    public string Url { get; set; } = string.Empty;

    public List<string> Actions { get; set; } = new();
}
