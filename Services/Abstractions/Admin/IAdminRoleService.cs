using Auth.Web.Application.Admin.Dtos;

namespace Auth.Web.Services.Abstractions.Admin;

public interface IAdminRoleService
{
    Task<IReadOnlyCollection<RoleAdminDto>> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<RoleAdminDto?> GetRoleByIdAsync(string roleId, CancellationToken cancellationToken = default);
    Task<string> CreateRoleAsync(string name, CancellationToken cancellationToken = default);
    Task RenameRoleAsync(string roleId, string newName, CancellationToken cancellationToken = default);
    Task DeleteRoleAsync(string roleId, CancellationToken cancellationToken = default);
}
