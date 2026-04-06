using Auth.Web.Data.Entities;
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

    public async Task<Area?> CreateAsync(string name, CancellationToken ct = default)
        => string.IsNullOrWhiteSpace(name) ? null : await _repository.CreateAsync(name, ct);

    public Task<bool> UpdateNameAsync(int id, string name, CancellationToken ct = default)
        => _repository.UpdateNameAsync(id, name, ct);

    public Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        => _repository.DeleteAsync(id, ct);

    async Task<IReadOnlyCollection<AreaAdminDto>> IAdminAreaService.GetAreasAsync(CancellationToken cancellationToken)
    {
        var areas = await _repository.GetAreasAsync(cancellationToken);
        if (areas.Count == 0)
        {
            return Array.Empty<AreaAdminDto>();
        }
        var counts = await _repository.GetAreaUserCountsAsync(cancellationToken);
        var clientMap = await _repository.GetAreaClientMappingAsync(cancellationToken);
        return areas.Select(area =>
        {
            clientMap.TryGetValue(area.Id, out var client);
            return MapArea(area, counts.TryGetValue(area.Id, out var count) ? count : 0, client.ClientId == 0 ? null : client.ClientId, client.Audience);
        }).ToList();
    }

    async Task<AreaAdminDto?> IAdminAreaService.GetAreaByIdAsync(int areaId, CancellationToken cancellationToken)
    {
        var area = await _repository.GetByIdAsync(areaId, cancellationToken);
        if (area is null)
        {
            return null;
        }
        var count = await _repository.GetAreaUserCountAsync(areaId, cancellationToken);
        var clientMap = await _repository.GetAreaClientMappingAsync(cancellationToken);
        clientMap.TryGetValue(areaId, out var client);
        return MapArea(area, count, client.ClientId == 0 ? null : client.ClientId, client.Audience);
    }

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

    private static AreaAdminDto MapArea(Area area, int userCount, int? clientId = null, string? clientName = null) => new()
    {
        Id = area.Id,
        Name = area.Name,
        UserCount = userCount,
        ClientId = clientId,
        ClientName = clientName
    };
}
