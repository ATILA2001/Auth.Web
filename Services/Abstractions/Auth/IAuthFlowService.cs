using Auth.Web.Application.Dtos;

namespace Auth.Web.Services.Abstractions.Auth;

public interface IAuthFlowService
{
    Task<LoginOutcome> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
}
