using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Auth.Web.Data;
using Auth.Web.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Auth.Web.Repositories.Implementations;

public sealed class AreaRepository : IAreaRepository
{
    private readonly AuthDbContext _db;

    public AreaRepository(AuthDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyDictionary<int, string>> GetAreaNamesAsync(CancellationToken ct = default)
    {
        var areas = await _db.Areas.AsNoTracking()
            .Select(a => new { a.Id, a.Name })
            .ToListAsync(ct);
        return areas.ToDictionary(a => a.Id, a => a.Name);
    }

    public Task<string?> GetAreaNameAsync(int areaId, CancellationToken ct = default)
        => _db.Areas.AsNoTracking()
            .Where(a => a.Id == areaId)
            .Select(a => a.Name)
            .FirstOrDefaultAsync(ct);
}
