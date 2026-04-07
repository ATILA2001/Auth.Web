using Auth.Web.Data;
using Auth.Web.Data.Entities;
using Auth.Web.Services.Abstractions.Permissions;
using Microsoft.EntityFrameworkCore;

namespace Auth.Web.Services.Implementations.Permissions;

public sealed class PermissionAuditService : IPermissionAuditService
{
    private readonly AuthDbContext _db;

    public PermissionAuditService(AuthDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(
        string actorUserId,
        string action,
        string detail,
        string? targetUserId = null,
        int? targetAreaId = null,
        CancellationToken ct = default)
    {
        _db.PermissionAuditLogs.Add(new PermissionAuditLog
        {
            Timestamp = DateTime.UtcNow,
            ActorUserId = actorUserId,
            Action = action,
            Detail = detail,
            TargetUserId = targetUserId,
            TargetAreaId = targetAreaId
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task IncrementAreaPermissionVersionAsync(int areaId, CancellationToken ct = default)
    {
        // UPDATE batch: incrementa PermissionVersion de todos los usuarios del área
        var userIds = await _db.UserAreas
            .Where(ua => ua.AreaId == areaId)
            .Select(ua => ua.UserId)
            .ToListAsync(ct);

        if (userIds.Count == 0) return;

        await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .ExecuteUpdateAsync(
                s => s.SetProperty(u => u.PermissionVersion, u => u.PermissionVersion + 1),
                ct);
    }

    public async Task IncrementUserPermissionVersionAsync(string userId, CancellationToken ct = default)
    {
        await _db.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(u => u.PermissionVersion, u => u.PermissionVersion + 1),
                ct);
    }
}
