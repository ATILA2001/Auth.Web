using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Auth.Web.Repositories.Abstractions;

public interface IAreaRepository
{
    Task<IReadOnlyDictionary<int, string>> GetAreaNamesAsync(CancellationToken ct = default);
    Task<string?> GetAreaNameAsync(int areaId, CancellationToken ct = default);
}
