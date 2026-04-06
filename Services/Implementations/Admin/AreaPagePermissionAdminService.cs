using Auth.Web.Application.Admin.Dtos;
using Auth.Web.Data.Entities;
using Auth.Web.Repositories.Abstractions.Admin;
using Auth.Web.Services.Abstractions.Admin;

namespace Auth.Web.Services.Implementations.Admin;

public sealed class AreaPagePermissionAdminService : IAdminAreaPagePermissionService
{
    private readonly IAreaPagePermissionAdminRepository _repository;
    private readonly IAreaAdminRepository _areaRepository;

    public AreaPagePermissionAdminService(
        IAreaPagePermissionAdminRepository repository,
        IAreaAdminRepository areaRepository)
    {
        _repository = repository;
        _areaRepository = areaRepository;
    }

    public async Task<IReadOnlyCollection<AreaPagePermissionAdminDto>> GetPermissionsAsync(CancellationToken cancellationToken = default)
    {
        var permissions = await _repository.GetAllAsync(cancellationToken);
        return permissions.Select(Map).ToList();
    }

    public async Task<IReadOnlyCollection<AreaPagePermissionAdminDto>> GetPermissionsByAreaAsync(int areaId, CancellationToken cancellationToken = default)
    {
        var permissions = await _repository.GetByAreaIdAsync(areaId, cancellationToken);
        return permissions.Select(Map).ToList();
    }

    public async Task<int> CreatePermissionAsync(int areaId, int? pageId, int? actionId, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.FindAsync(areaId, pageId, actionId, cancellationToken);
        if (existing is not null)
        {
            return existing.Id;
        }

        var entity = await _repository.CreateAsync(areaId, pageId, actionId, cancellationToken);
        return entity.Id;
    }

    public async Task UpdatePermissionAsync(int id, int areaId, int? pageId, int? actionId, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new InvalidOperationException($"Permiso de área con id {id} no encontrado.");
        }

        var duplicate = await _repository.FindAsync(areaId, pageId, actionId, cancellationToken);
        if (duplicate is not null && duplicate.Id != id)
        {
            throw new InvalidOperationException("Ya existe este permiso de área.");
        }

        await _repository.UpdateAsync(id, areaId, pageId, actionId, cancellationToken);
    }

    public Task DeletePermissionAsync(int id, CancellationToken cancellationToken = default)
        => _repository.DeleteAsync(id, cancellationToken);

    private static AreaPagePermissionAdminDto Map(AreaPagePermission e) => new()
    {
        Id = e.Id,
        AreaId = e.AreaId,
        AreaName = e.Area?.Name ?? e.AreaId.ToString(),
        PageId = e.PageId,
        PageName = e.Page?.Name ?? "Sin asignar",
        PageUrl = e.Page?.Url ?? string.Empty,
        ActionPermissionId = e.ActionPermissionId,
        ActionName = e.ActionPermission?.Name ?? "Sin asignar"
    };
}
