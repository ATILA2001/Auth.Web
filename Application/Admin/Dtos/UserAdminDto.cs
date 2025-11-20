namespace Auth.Web.Application.Admin.Dtos;

public sealed class UserAdminDto
{
    public string Id { get; init; } = default!;
    public string UserName { get; init; } = default!;
    public string? Email { get; init; }
    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> Areas { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<int> AreaIds { get; init; } = Array.Empty<int>(); // TODO: UI debe usar AreaIds para edición; Areas solo para display
}
