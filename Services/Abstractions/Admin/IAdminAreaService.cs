using Auth.Web.Application.Admin.Dtos;

namespace Auth.Web.Services.Abstractions.Admin;

public interface IAdminAreaService
{
    Task<IReadOnlyCollection<AreaAdminDto>> GetAreasAsync(CancellationToken cancellationToken = default);
    Task<AreaAdminDto?> GetAreaByIdAsync(int areaId, CancellationToken cancellationToken = default);
    Task<int> CreateAreaAsync(string name, CancellationToken cancellationToken = default);
    Task UpdateAreaAsync(int areaId, string name, CancellationToken cancellationToken = default);
    Task DeleteAreaAsync(int areaId, CancellationToken cancellationToken = default);
}
