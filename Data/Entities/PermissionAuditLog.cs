namespace Auth.Web.Data.Entities;

public class PermissionAuditLog
{
    public int Id { get; set; }

    public DateTime Timestamp { get; set; }

    public string ActorUserId { get; set; } = string.Empty;

    public string? TargetUserId { get; set; }

    public int? TargetAreaId { get; set; }

    /// <summary>AreaAssigned, OverrideGranted, OverrideDenied, AreaPermissionChanged, PermissionVersionIncremented</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>JSON con el detalle del cambio (antes/después)</summary>
    public string Detail { get; set; } = string.Empty;
}
