using Auth.Web.Domain.Entities;

namespace Auth.Web.Services.Admin;

public interface IUserAdminService
{
    Task<(List<UserItem> Users, List<string> AllRoles)> GetAsync(CancellationToken ct = default);
    Task<bool> UpdateRolesAsync(string userId, IEnumerable<string> roles, CancellationToken ct = default);
}

public sealed record UserItem(string Id, string? UserName, string? Email, List<string> Roles);
