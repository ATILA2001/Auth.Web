using Auth.Web.Application.Admin.Dtos;

namespace Auth.Web.Services.Abstractions.Admin;

public interface IAdminActionPermissionService
{
    Task<IReadOnlyCollection<ActionPermissionAdminDto>> GetActionsAsync(CancellationToken cancellationToken = default);
    Task<ActionPermissionAdminDto?> GetActionByIdAsync(int actionId, CancellationToken cancellationToken = default);
    Task<int> CreateActionAsync(string name, CancellationToken cancellationToken = default);
    Task UpdateActionAsync(int actionId, string name, CancellationToken cancellationToken = default);
    Task DeleteActionAsync(int actionId, CancellationToken cancellationToken = default);
}
