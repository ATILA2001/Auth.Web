using Auth.Web.Application.Admin.Dtos;
using Auth.Web.Data.Entities;
using Auth.Web.Repositories.Abstractions.Admin;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Services.Abstractions.Permissions;

namespace Auth.Web.Services.Implementations.Admin;

public sealed class UserPageOverrideAdminService : IAdminUserPageOverrideService
{
    private readonly IUserPageOverrideAdminRepository _repository;
    private readonly IPermissionAuditService _auditService;

    public UserPageOverrideAdminService(
        IUserPageOverrideAdminRepository repository,
        IPermissionAuditService auditService)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
        _auditService = auditService;
    }

    public async Task<IReadOnlyCollection<UserPageOverrideAdminDto>> GetOverridesByUserAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Array.Empty<UserPageOverrideAdminDto>();

        var entities = await _repository.GetByUserIdAsync(userId, ct);
        return entities.Select(Map).ToArray();
    }

    public async Task<int> CreateOverrideAsync(string userId, int? pageId, int? actionPermissionId, bool isAllowed, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId is required.", nameof(userId));

        // Enforce DB check constraint: allowed overrides require ActionPermissionId.
        if (isAllowed && actionPermissionId is null)
            throw new ArgumentException("Allowed override requires an ActionPermissionId.", nameof(actionPermissionId));

        var existing = await _repository.FindAsync(userId, pageId, actionPermissionId, ct);
        if (existing is not null)
            throw new InvalidOperationException("Ya existe un override identico para este usuario.");

        var id = await _repository.CreateAsync(userId, pageId, actionPermissionId, isAllowed, ct);
        await _auditService.IncrementUserPermissionVersionAsync(userId, ct);
        return id;
    }

    public async Task DeleteOverrideAsync(int id, CancellationToken ct = default)
    {
        var entity = await _repository.GetByIdAsync(id, ct);
        await _repository.DeleteAsync(id, ct);
        if (entity is not null)
            await _auditService.IncrementUserPermissionVersionAsync(entity.UserId, ct);
    }

    private static UserPageOverrideAdminDto Map(UserPageOverride o) => new()
    {
        Id = o.Id,
        UserId = o.UserId,
        PageId = o.PageId,
        PageName = o.Page?.Name ?? "Sin asignar",
        PageUrl = o.Page?.Url ?? string.Empty,
        ActionPermissionId = o.ActionPermissionId,
        ActionName = o.ActionPermission?.Name ?? "Sin asignar",
        IsAllowed = o.IsAllowed
    };
}
