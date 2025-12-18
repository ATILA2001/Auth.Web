using Auth.Web.Application.Users.Registration;

namespace Auth.Web.Services.Abstractions.Users;

public interface IUserRegistrationService
{
    Task<RegisterUserResult> RegisterUserAsync(RegisterUserRequest request, CancellationToken cancellationToken = default);
}
