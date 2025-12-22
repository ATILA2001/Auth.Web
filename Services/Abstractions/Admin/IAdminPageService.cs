using Auth.Web.Application.Admin.Dtos;

namespace Auth.Web.Services.Abstractions.Admin;

public interface IAdminPageService
{
    Task<IReadOnlyCollection<PageAdminDto>> GetPagesAsync(CancellationToken cancellationToken = default);
    Task<PageAdminDto?> GetPageByIdAsync(int pageId, CancellationToken cancellationToken = default);
    Task<int> CreatePageAsync(string name, string url, CancellationToken cancellationToken = default);
    Task UpdatePageAsync(int pageId, string name, string url, CancellationToken cancellationToken = default);
    Task DeletePageAsync(int pageId, CancellationToken cancellationToken = default);
}
