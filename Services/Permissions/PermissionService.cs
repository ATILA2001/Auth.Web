using Auth.Web.Domain.Dtos;
using Auth.Web.Services.Abstractions;
using Auth.Web.Utils;

namespace Auth.Web.Services.Permissions;

public class PermissionService : IPermissionService
{
    public Task<UserPermissionsDto> GetAsync(string userName)
    {
        var dto = new UserPermissionsDto
        {
            Pages = new List<PagePermissionDto>
            {
                new()
                {
                    Url = Urls.NormalizePagePath("/admin/dashboard"),
                    Actions = new List<string> { "Ver", "Editar" }
                }
            },
            Areas = new List<int> { 1 },
            Version = 1
        };

        return Task.FromResult(dto);
    }
}
