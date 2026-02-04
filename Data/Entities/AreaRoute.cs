namespace Auth.Web.Data.Entities;

public class AreaRoute
{
    public int Id { get; set; }

    public int? AreaId { get; set; }

    public int? ClientId { get; set; }

    public Area? Area { get; set; }

    public ApplicationClient? Client { get; set; }

    // Menor prioridad => se evalóa primero
    public int Priority { get; set; } = 1;

    public bool IsActive { get; set; } = true;
}
