using Microsoft.AspNetCore.Identity;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Application.Admin.Dtos;
using Auth.Web.Repositories.Abstractions.Admin;

namespace Auth.Web.Services.Implementations.Admin;

public sealed class RoleAdminService : IAdminRoleService
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IRoleAdminRepository _repository;

    public RoleAdminService(RoleManager<IdentityRole> roleManager, IRoleAdminRepository repository)
    {
        _roleManager = roleManager;
        _repository = repository;
    }

    public Task<List<IdentityRole>> GetRolesAsync(CancellationToken ct = default)
        => _repository.GetRolesAsync(ct);

    public async Task<bool> CreateRoleAsync(string name, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        if (await _roleManager.RoleExistsAsync(name)) return true;
        var res = await _roleManager.CreateAsync(new IdentityRole(name));
        return res.Succeeded;
    }

    public async Task<bool> DeleteRoleAsync(string roleId, CancellationToken ct = default)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role is null) return false;
        var res = await _roleManager.DeleteAsync(role);
        return res.Succeeded;
    }

    Task<IReadOnlyCollection<RoleAdminDto>> IAdminRoleService.GetRolesAsync(CancellationToken cancellationToken)
        => _repository.GetRolesWithUserCountAsync(cancellationToken);

    Task<RoleAdminDto?> IAdminRoleService.GetRoleByIdAsync(string roleId, CancellationToken cancellationToken)
        => _repository.GetRoleByIdWithUserCountAsync(roleId, cancellationToken);

    async Task<string> IAdminRoleService.CreateRoleAsync(string name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
        if (await _roleManager.RoleExistsAsync(name))
        {
            var existing = await _roleManager.FindByNameAsync(name);
            return existing?.Id ?? string.Empty;
        }
        var res = await _roleManager.CreateAsync(new IdentityRole(name.Trim()));
        return res.Succeeded ? (await _roleManager.FindByNameAsync(name.Trim()))!.Id : string.Empty;
    }

    async Task IAdminRoleService.RenameRoleAsync(string roleId, string newName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(newName)) return;
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role is null) return;
        role.Name = newName.Trim();
        await _roleManager.UpdateAsync(role);
    }

    async Task IAdminRoleService.DeleteRoleAsync(string roleId, CancellationToken cancellationToken)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role is null) return;
        await _roleManager.DeleteAsync(role);
    }
}
