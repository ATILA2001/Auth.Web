using Auth.Web.Application.Dtos;

namespace Auth.Web.Application.Users;

public interface IUserRegistrationService
{
    Task<RegisterUserResult> RegisterUserAsync(RegisterUserRequest request, CancellationToken cancellationToken = default);
}