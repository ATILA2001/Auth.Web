using Auth.Web.Application.Admin.Dtos;
using Auth.Web.Data.Entities;
using Auth.Web.Repositories.Abstractions.Admin;
using Auth.Web.Services.Abstractions.Admin;

namespace Auth.Web.Services.Implementations.Admin;

public sealed class UserPageOverrideAdminService : IAdminUserPageOverrideService
{
    private readonly IUserPageOverrideAdminRepository _repository;

    public UserPageOverrideAdminService(IUserPageOverrideAdminRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public async Task<IReadOnlyCollection<UserPageOverrideAdminDto>> GetOverridesByUserAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Array.Empty<UserPageOverrideAdminDto>();

        var entities = await _repository.GetByUserIdAsync(userId, ct);
        return entities.Select(Map).ToArray();
    }

    public async Task<int> CreateOverrideAsync(string userId, int? pageId, int? actionPermissionId, string type, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId is required.", nameof(userId));

        if (type != "GRANT" && type != "DENY")
            throw new ArgumentException("Type must be GRANT or DENY.", nameof(type));

        // Enforce DB check constraint: GRANT requires ActionPermissionId
        if (type == "GRANT" && actionPermissionId is null)
            throw new ArgumentException("GRANT override requires an ActionPermissionId.", nameof(actionPermissionId));

        var existing = await _repository.FindAsync(userId, pageId, actionPermissionId, ct);
        if (existing is not null)
            throw new InvalidOperationException("Ya existe un override idéntico para este usuario.");

        return await _repository.CreateAsync(userId, pageId, actionPermissionId, type, ct);
    }

    public Task DeleteOverrideAsync(int id, CancellationToken ct = default)
        => _repository.DeleteAsync(id, ct);

    private static UserPageOverrideAdminDto Map(UserPageOverride o) => new()
    {
        Id = o.Id,
        UserId = o.UserId,
        PageId = o.PageId,
        PageName = o.Page?.Name ?? "Sin asignar",
        PageUrl = o.Page?.Url ?? string.Empty,
        ActionPermissionId = o.ActionPermissionId,
        ActionName = o.ActionPermission?.Name ?? "Sin asignar",
        Type = o.Type
    };
}
