using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Application.Admin.Dtos;
using Auth.Web.Repositories.Abstractions.Admin;
using Auth.Web.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Auth.Web.Services.Implementations.Admin;

public sealed class UserAdminService : IAdminUserService
{
    private readonly IUserAdminRepository _repository;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserAdminService(IUserAdminRepository repository, UserManager<ApplicationUser> userManager)
    {
        _repository = repository;
        _userManager = userManager;
    }

    public Task<IReadOnlyCollection<UserAdminDto>> GetUsersAsync(CancellationToken cancellationToken = default)
        => _repository.GetUsersAsync(cancellationToken);

    public Task<UserAdminDto?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
        => _repository.GetUserByIdAsync(userId, cancellationToken);

    public async Task UpdateUserRolesAndAreasAsync(string userId, IEnumerable<string> roles, IEnumerable<int> areaIds, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return;

        var currentRoles = await _userManager.GetRolesAsync(user);
        var desiredRoles = roles.Distinct().ToArray();
        var toAddRoles = desiredRoles.Except(currentRoles).ToArray();
        var toRemoveRoles = currentRoles.Except(desiredRoles).ToArray();
        if (toAddRoles.Length > 0) await _userManager.AddToRolesAsync(user, toAddRoles);
        if (toRemoveRoles.Length > 0) await _userManager.RemoveFromRolesAsync(user, toRemoveRoles);

        await _repository.UpdateUserAreasAsync(userId, areaIds, cancellationToken);
    }
}
