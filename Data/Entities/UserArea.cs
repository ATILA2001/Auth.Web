namespace Auth.Web.Data.Entities;

public class UserArea
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public int AreaId { get; set; }

    public Area? Area { get; set; }
}
