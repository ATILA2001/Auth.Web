using Auth.Web.Application.Admin.Dtos;
using Auth.Web.Domain.Entities;

namespace Auth.Web.Repositories.Abstractions.Admin;

public interface IAreaAdminRepository
{
    Task<List<Area>> GetAreasAsync(CancellationToken ct = default);
    Task<Area?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Area> CreateAsync(string name, CancellationToken ct = default);
    Task<bool> UpdateNameAsync(int id, string name, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyCollection<AreaAdminDto>> GetAreasWithUserCountAsync(CancellationToken ct = default);
    Task<AreaAdminDto?> GetAreaWithUserCountByIdAsync(int areaId, CancellationToken ct = default);
}
