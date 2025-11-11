using Auth.Web.Domain.Dtos;
using Auth.Web.Domain.Entities;

namespace Auth.Web.Services.Abstractions;

public interface ITokenService
{
    Task<string> CreateAsync(ApplicationUser user, IEnumerable<string> roles, UserPermissionsDto permissions, string audience);
}
