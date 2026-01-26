using Auth.Web.Application.Permissions.Dtos;

namespace Auth.Web.Application.Auth;

public sealed class AuthClaimsModel
{
    public string UserId { get; init; } = default!;
    public string? Email { get; init; }
    public string? DisplayName { get; init; }
    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> Areas { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> Apps { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<PagePermissionDto> Pages { get; init; } = Array.Empty<PagePermissionDto>();
    public int PermissionsVersion { get; init; } = 1;
    public string PermissionsJson { get; init; } = "{}";
    public string? FirstPageUrl { get; init; }
}
