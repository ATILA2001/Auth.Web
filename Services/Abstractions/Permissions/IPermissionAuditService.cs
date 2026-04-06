namespace Auth.Web.Services.Abstractions.Permissions;

public interface IPermissionAuditService
{
    Task LogAsync(
        string actorUserId,
        string action,
        string detail,
        string? targetUserId = null,
        int? targetAreaId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Incrementa PermissionVersion de todos los usuarios del área indicada.
    /// Llamar al cambiar cualquier AreaPagePermission de esa área.
    /// </summary>
    Task IncrementAreaPermissionVersionAsync(int areaId, CancellationToken ct = default);
}
