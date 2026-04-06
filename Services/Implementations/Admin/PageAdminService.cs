using Auth.Web.Data.Entities;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Application.Admin.Dtos;
using Auth.Web.Repositories.Abstractions.Admin;

namespace Auth.Web.Services.Implementations.Admin;

public sealed class PageAdminService : IAdminPageService
{
    private readonly IPageAdminRepository _repository;

    public PageAdminService(IPageAdminRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyCollection<PageAdminDto>> GetPagesAsync(CancellationToken cancellationToken = default)
    {
        var pages = await _repository.GetPagesAsync(cancellationToken);
        if (pages.Count == 0)
        {
            return Array.Empty<PageAdminDto>();
        }
        var counts = await _repository.GetPagePermissionCountsAsync(cancellationToken);
        return pages.Select(p => MapPage(p, counts.TryGetValue(p.Id, out var count) ? count : 0)).ToList();
    }

    public async Task<PageAdminDto?> GetPageByIdAsync(int pageId, CancellationToken cancellationToken = default)
    {
        var page = await _repository.GetByIdAsync(pageId, cancellationToken);
        if (page is null)
        {
            return null;
        }
        var count = await _repository.GetPagePermissionCountAsync(pageId, cancellationToken);
        return MapPage(page, count);
    }

    public async Task<int> CreatePageAsync(string name, string url, int? clientId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(url)) return 0;
        var page = await _repository.CreateAsync(name, url, clientId, cancellationToken);
        return page.Id;
    }

    public Task UpdatePageAsync(int pageId, string name, string url, int? clientId, CancellationToken cancellationToken = default)
        => _repository.UpdateAsync(pageId, name, url, clientId, cancellationToken);

    public Task DeletePageAsync(int pageId, CancellationToken cancellationToken = default)
        => _repository.DeleteAsync(pageId, cancellationToken);

    private static PageAdminDto MapPage(Page page, int permissionCount) => new()
    {
        Id = page.Id,
        Name = page.Name,
        Url = page.Url,
        ClientId = page.ClientId,
        ClientName = page.Client?.ClientId ?? string.Empty,
        PermissionCount = permissionCount
    };
}
