using Auth.Web.Data.Entities;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Application.Admin.Dtos;
using Auth.Web.Repositories.Abstractions.Admin;

namespace Auth.Web.Services.Implementations.Admin;

public sealed class ActionPermissionAdminService : IAdminActionPermissionService
{
    private readonly IActionPermissionAdminRepository _repository;

    public ActionPermissionAdminService(IActionPermissionAdminRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyCollection<ActionPermissionAdminDto>> GetActionsAsync(CancellationToken cancellationToken = default)
    {
        var actions = await _repository.GetActionsAsync(cancellationToken);
        if (actions.Count == 0)
        {
            return Array.Empty<ActionPermissionAdminDto>();
        }
        var counts = await _repository.GetActionUsageCountsAsync(cancellationToken);
        return actions.Select(a => MapAction(a, counts.TryGetValue(a.Id, out var count) ? count : 0)).ToList();
    }

    public async Task<ActionPermissionAdminDto?> GetActionByIdAsync(int actionId, CancellationToken cancellationToken = default)
    {
        var action = await _repository.GetByIdAsync(actionId, cancellationToken);
        if (action is null)
        {
            return null;
        }
        var count = await _repository.GetActionUsageCountAsync(actionId, cancellationToken);
        return MapAction(action, count);
    }

    public async Task<int> CreateActionAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) return 0;
        var action = await _repository.CreateAsync(name, cancellationToken);
        return action.Id;
    }

    public Task UpdateActionAsync(int actionId, string name, CancellationToken cancellationToken = default)
        => _repository.UpdateAsync(actionId, name, cancellationToken);

    public Task DeleteActionAsync(int actionId, CancellationToken cancellationToken = default)
        => _repository.DeleteAsync(actionId, cancellationToken);

    private static ActionPermissionAdminDto MapAction(ActionPermission action, int usageCount) => new()
    {
        Id = action.Id,
        Name = action.Name,
        UsageCount = usageCount
    };
}
