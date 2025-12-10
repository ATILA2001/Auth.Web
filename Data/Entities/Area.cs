namespace Auth.Web.Data.Entities;

public class Area
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<UserArea> UserAreas { get; set; } = new List<UserArea>();
}
