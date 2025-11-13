namespace Auth.Web.Domain.Entities;

public class AreaRoute
{
    public int Id { get; set; }

    public int AreaId { get; set; }

    // ClientId de ApplicationClient (FK lógica por string)
    public string ClientId { get; set; } = string.Empty;

    // Debe estar incluida en AllowedReturnUrlsJson del cliente
    public string ReturnUrl { get; set; } = string.Empty;

    // Menor prioridad => se evalúa primero
    public int Priority { get; set; } = 1;

    public bool IsActive { get; set; } = true;
}
