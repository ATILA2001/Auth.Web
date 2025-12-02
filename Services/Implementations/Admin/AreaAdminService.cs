using Auth.Web.Domain.Entities;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Application.Admin.Dtos;
using Auth.Web.Repositories.Abstractions.Admin;

namespace Auth.Web.Services.Implementations.Admin;

public sealed class AreaAdminService : IAdminAreaService
{
    private readonly IAreaAdminRepository _repository;
    
    public AreaAdminService(IAreaAdminRepository repository)
    {
        _repository = repository;
    }

    public Task<List<Area>> GetAreasAsync(CancellationToken ct = default)
        => _repository.GetAreasAsync(ct);

    public Task<Area?> CreateAsync(string name, CancellationToken ct = default)
        => string.IsNullOrWhiteSpace(name) ? Task.FromResult<Area?>(null) : _repository.CreateAsync(name, ct);

    public Task<bool> UpdateNameAsync(int id, string name, CancellationToken ct = default)
        => _repository.UpdateNameAsync(id, name, ct);

    public Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        => _repository.DeleteAsync(id, ct);

    async Task<IReadOnlyCollection<AreaAdminDto>> IAdminAreaService.GetAreasAsync(CancellationToken cancellationToken)
        => await _repository.GetAreasWithUserCountAsync(cancellationToken);

    Task<AreaAdminDto?> IAdminAreaService.GetAreaByIdAsync(int areaId, CancellationToken cancellationToken)
        => _repository.GetAreaWithUserCountByIdAsync(areaId, cancellationToken);

    async Task<int> IAdminAreaService.CreateAreaAsync(string name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name)) return 0;
        var area = await _repository.CreateAsync(name, cancellationToken);
        return area.Id;
    }

    Task IAdminAreaService.UpdateAreaAsync(int areaId, string name, CancellationToken cancellationToken)
        => _repository.UpdateNameAsync(areaId, name, cancellationToken);

    Task IAdminAreaService.DeleteAreaAsync(int areaId, CancellationToken cancellationToken)
        => _repository.DeleteAsync(areaId, cancellationToken);
}
