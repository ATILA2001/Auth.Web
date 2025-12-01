using Auth.Web.Data;
using Auth.Web.Repositories.Abstractions;

namespace Auth.Web.Repositories.Implementations;

/// <summary>
/// EF Core repository placeholder. Not wired in DI yet.
/// </summary>
public sealed class RouteRepository : IRouteRepository
{
    private readonly AuthDbContext _db;

    public RouteRepository(AuthDbContext db)
    {
        _db = db;
    }

    // TODO: Extract logic from services into repository methods
}
